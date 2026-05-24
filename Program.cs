using System.Security.Cryptography;
using System.Threading.RateLimiting;
using GuildfordBsac.Web.Common;
using GuildfordBsac.Web.Controllers;
using GuildfordBsac.Web.Properties;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Rewrite;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<FacebookService>();
builder.Services.AddScoped<IReCaptchaValidator, ReCaptchaValidator>();
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
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.Always;
});

var app = builder.Build();

app.UsePathBase("/gbsacCore");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCookiePolicy();

app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
    context.Items["CspNonce"] = nonce;
    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "unsafe-none";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] =
        $"default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}' https://www.google.com https://www.gstatic.com https://www.google-analytics.com; " +
        $"style-src 'self' 'nonce-{nonce}' https://fonts.googleapis.com https://maxcdn.bootstrapcdn.com https://cdn.jsdelivr.net; " +
        $"style-src-attr 'unsafe-inline'; " +
        $"font-src 'self' https://fonts.gstatic.com https://maxcdn.bootstrapcdn.com https://cdn.jsdelivr.net; " +
        $"img-src 'self' data: https://www.google-analytics.com https://www.gstatic.com https://*.fbcdn.net; " +
        $"frame-src https://www.google.com https://recaptcha.google.com; " +
        $"connect-src 'self' https://www.google-analytics.com https://region1.google-analytics.com; " +
        $"form-action 'self' https://guildford-bsac.us14.list-manage.com; " +
        $"worker-src 'none'; " +
        $"manifest-src 'self'; " +
        $"object-src 'none'; " +
        $"base-uri 'self'; " +
        $"frame-ancestors 'self'; " +
        $"report-uri /csp-report;";
    await next();
});

app.UseStaticFiles();

app.UseRewriter(new RewriteOptions()
    .AddRedirect("^home/YearPlanner$", "YearPlanner", 301));

app.UseRouting();
app.UseOutputCache();
app.UseRateLimiter();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.MapPost("/csp-report", async (HttpContext ctx, ILogger<Program> logger) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var report = await reader.ReadToEndAsync();
    logger.LogWarning("CSP violation: {Report}", report);
    return Results.NoContent();
});

RotativaConfiguration.Setup(app.Environment.ContentRootPath, "Rotativa");

app.Run();

public partial class Program { }
