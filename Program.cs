using System.Security.Cryptography;
using System.Threading.RateLimiting;
using GuildfordBsac.Web.Common;
using GuildfordBsac.Web.Configuration;
using GuildfordBsac.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
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

    builder.Services.AddOptions<FacebookSettings>()
        .Bind(builder.Configuration.GetSection("Facebook"));

    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });
    builder.Services.AddSingleton<IFacebookService, FacebookService>();
    builder.Services.AddHttpClient("recaptcha", c => c.Timeout = TimeSpan.FromSeconds(10));
    builder.Services.AddHttpClient("facebook", c => c.Timeout = TimeSpan.FromSeconds(5));
    builder.Services.AddScoped<IReCaptchaValidator, ReCaptchaValidator>();

    builder.Services.AddSingleton<IGoogleCalendarClient, GoogleCalendarApiService>();
    builder.Services.AddSingleton<IEmailService, GoogleEmailApiService>();
    builder.Services.AddScoped<IContactFormService, ContactFormService>();
    builder.Services.AddSingleton<PngRenderLock>();

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
        .AddCheck<DataFilesHealthCheck>("data-files")
        .AddCheck<ConfigurationHealthCheck>("configuration")
        .AddCheck<RotativaBinaryHealthCheck>("rotativa");

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

    // Warn on missing runtime-injected secrets in non-Development environments.
    // These are injected via web.config at deploy time; an empty value means the deploy
    // variable was not set, and the affected feature will silently fail on first use.
    if (!app.Environment.IsDevelopment())
    {
        var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;
        if (string.IsNullOrWhiteSpace(appSettings.ServiceAccount.PrivateKey))
            startupLogger.LogError("AppSettings.ServiceAccount.PrivateKey is not configured — Google Calendar and Gmail integrations will fail");
        if (string.IsNullOrWhiteSpace(appSettings.RecaptchaApiKey))
            startupLogger.LogError("AppSettings.RecaptchaApiKey is not configured — reCAPTCHA validation will fail on contact form submissions");
        if (string.IsNullOrWhiteSpace(appSettings.ContactEmail))
            startupLogger.LogError("AppSettings.ContactEmail is not configured — contact form email delivery will fail");
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
            // Static files bypass the security-header middleware (which runs later), so
            // headers that don't require a CSP nonce are applied here directly.
            ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
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

// Validates that runtime-injected secrets are present. Only meaningful in non-Development
// environments; returns Healthy in Development so tests and local runs are unaffected.
internal class ConfigurationHealthCheck : IHealthCheck
{
    private readonly AppSettings _settings;
    private readonly IWebHostEnvironment _env;

    public ConfigurationHealthCheck(IOptions<AppSettings> settings, IWebHostEnvironment env)
    {
        _settings = settings.Value;
        _env = env;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_env.IsDevelopment())
            return Task.FromResult(HealthCheckResult.Healthy("Skipped in Development"));

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_settings.ServiceAccount.PrivateKey))
            missing.Add("AppSettings__ServiceAccount__PrivateKey");
        if (string.IsNullOrWhiteSpace(_settings.RecaptchaApiKey))
            missing.Add("AppSettings__RecaptchaApiKey");
        if (string.IsNullOrWhiteSpace(_settings.ContactEmail))
            missing.Add("AppSettings__ContactEmail");

        return missing.Count == 0
            ? Task.FromResult(HealthCheckResult.Healthy())
            : Task.FromResult(HealthCheckResult.Degraded($"Missing runtime config: {string.Join(", ", missing)}"));
    }
}

// Verifies the Rotativa wkhtmltopdf binaries are present so PDF/PNG export failures
// surface at the health probe rather than on the first user request.
internal class RotativaBinaryHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _env;

    public RotativaBinaryHealthCheck(IWebHostEnvironment env) => _env = env;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var rotativaPath = Path.Combine(_env.ContentRootPath, "Rotativa");
        var missing = new[] { "wkhtmltopdf.exe", "wkhtmltoimage.exe" }
            .Where(f => !File.Exists(Path.Combine(rotativaPath, f)))
            .ToList();

        return missing.Count == 0
            ? Task.FromResult(HealthCheckResult.Healthy())
            : Task.FromResult(HealthCheckResult.Unhealthy($"Missing Rotativa binaries: {string.Join(", ", missing)}"));
    }
}
