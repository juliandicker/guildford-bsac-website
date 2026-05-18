using GuildfordBsac.Web.Controllers;
using GuildfordBsac.Web.Properties;
using Microsoft.AspNetCore.Rewrite;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<FacebookService>();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddOutputCache();

var app = builder.Build();

app.UsePathBase("/gbsacCore");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseStaticFiles();

app.UseRewriter(new RewriteOptions()
    .AddRedirect("^home/YearPlanner$", "YearPlanner", 301));

app.UseRouting();
app.UseOutputCache();
app.UseAuthorization();

app.MapDefaultControllerRoute();

RotativaConfiguration.Setup(app.Environment.ContentRootPath, "Rotativa");

app.Run();
