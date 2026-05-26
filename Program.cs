using System.Security.Cryptography;
using System.Threading.RateLimiting;
using GuildfordBsac.Web.Common;
using GuildfordBsac.Web.Configuration;
using GuildfordBsac.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rotativa.AspNetCore;
using Serilog;
using Serilog.Events;

// Configure Serilog before anything else so startup errors are captured
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddOptions<AppSettings>()
        .Bind(builder.Configuration.GetSection("AppSettings"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
    builder.Services.AddSingleton<IFacebookService, FacebookService>();
    builder.Services.AddHttpClient("recaptcha", c => c.Timeout = TimeSpan.FromSeconds(10));
    builder.Services.AddHttpClient("facebook", c => c.Timeout = TimeSpan.FromSeconds(5));
    builder.Services.AddScoped<IReCaptchaValidator, ReCaptchaValidator>();

    // GoogleApiService implements both IGoogleCalendarClient and IEmailService via shared singleton
    builder.Services.AddSingleton<GoogleApiService>();
    builder.Services.AddSingleton<IGoogleCalendarClient>(sp => sp.GetRequiredService<GoogleApiService>());
    builder.Services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<GoogleApiService>());

    builder.Services.AddSingleton<ICalendarService, CalendarService>();
    builder.Services.AddSingleton<ISvgIconProvider, SvgIconProvider>();
    builder.Services.AddSingleton<IFaqService, FaqService>();
    builder.Services.AddSingleton<IMembershipRatesService>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        return new MembershipRatesService(Path.Combine(env.ContentRootPath, "App_Data", "membershiprates.json"));
    });
    builder.Services.AddSingleton<ITeamService>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        return new TeamService(Path.Combine(env.ContentRootPath, "App_Data", "team.json"));
    });
    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
    builder.Services.AddOutputCache();
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("contact", o =>
        {
            o.PermitLimit = 5;
            o.Window = TimeSpan.FromMinutes(10);
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("pdf", o =>
        {
            o.PermitLimit = 3;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("yearplanner", o =>
        {
            o.PermitLimit = 30;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.Secure = CookieSecurePolicy.Always;
    });
    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
    builder.Services.AddHealthChecks()
        .AddCheck<DataFilesHealthCheck>("data-files");

    var app = builder.Build();

    // Validate required data files at startup — surfaces missing/malformed JSON before first request
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        _ = app.Services.GetRequiredService<IMembershipRatesService>().MembershipRates;
        _ = app.Services.GetRequiredService<ITeamService>().TeamMembers;
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(ex, "Startup data validation failed — app cannot start");
        throw;
    }

    app.UsePathBase("/gbsacCore");

    app.UseResponseCompression();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseCookiePolicy();

    // Static files are served before the security-header middleware to avoid
    // generating a CSP nonce on every asset request (nonce is only meaningful in HTML).
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // 7-day cache for local assets (not fingerprinted, so immutable is omitted)
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=604800";
        }
    });

    // Pre-built once — only the nonce placeholder changes per request
    var cspTemplate =
        "default-src 'self'; " +
        "script-src 'self' 'nonce-{0}' https://www.google.com https://www.gstatic.com; " +
        "style-src 'self' 'nonce-{0}' https://fonts.googleapis.com https://maxcdn.bootstrapcdn.com https://cdn.jsdelivr.net; " +
        "style-src-attr 'unsafe-inline'; " +
        "font-src 'self' https://fonts.gstatic.com https://maxcdn.bootstrapcdn.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https://www.gstatic.com https://*.fbcdn.net; " +
        "frame-src https://www.google.com https://recaptcha.google.com; " +
        "connect-src 'self' https://www.google.com https://www.gstatic.com; " +
        "form-action 'self' https://guildford-bsac.us14.list-manage.com; " +
        "worker-src 'none'; " +
        "manifest-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'self';";

    app.Use(async (context, next) =>
    {
        var nonceBytes = new byte[18];
        RandomNumberGenerator.Fill(nonceBytes);
        var nonce = Convert.ToBase64String(nonceBytes);
        context.Items["CspNonce"] = nonce;
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Cross-Origin-Embedder-Policy"] = "unsafe-none";
        context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        context.Response.Headers["Content-Security-Policy"] = string.Format(cspTemplate, nonce);
        await next();
    });

    app.UseRewriter(new RewriteOptions()
        .AddRedirect("^home/YearPlanner$", "YearPlanner", 301));

    app.UseRouting();
    app.UseOutputCache();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapDefaultControllerRoute();
    app.MapHealthChecks("/health");

    RotativaConfiguration.Setup(app.Environment.ContentRootPath, "Rotativa");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

internal class DataFilesHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _env;

    public DataFilesHealthCheck(IWebHostEnvironment env) => _env = env;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var required = new[] { "membershiprates.json", "team.json", "faqs.json", "faqsContact.json" };
        foreach (var f in required)
        {
            if (!File.Exists(Path.Combine(_env.ContentRootPath, "App_Data", f)))
                return Task.FromResult(HealthCheckResult.Unhealthy($"Missing App_Data file: {f}"));
        }
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
