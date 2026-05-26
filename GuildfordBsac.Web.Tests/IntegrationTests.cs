using GuildfordBsac.Web.Common;
using GuildfordBsac.Web.Models;
using GuildfordBsac.Web.Properties;
using GuildfordBsac.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace GuildfordBsac.Web.Tests;

internal class AlwaysPassReCaptchaValidator : IReCaptchaValidator
{
    public Task<ReCaptchaResponse> ValidateAsync(HttpContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new ReCaptchaResponse { Success = true });
}

internal class NullFacebookService : IFacebookService
{
    public Task<List<FacebookPostModel>> GetRecentPostsAsync(int limit = 5, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<FacebookPostModel>());
}

internal class NullGoogleApiHelper : IGoogleApiHelper
{
    public Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Calendar>>(new List<Calendar>());
    public Task<bool> SendMessageAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public class GuildfordBsacWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Navigate from bin/Release|Debug/net8.0/ up to the repo root where App_Data lives
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        builder.UseContentRoot(webRoot);
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // CookieSecurePolicy.Always marks the antiforgery cookie as Secure; the test
            // HttpClient uses plain HTTP so CookieContainer won't send it — override for tests.
            services.Configure<CookiePolicyOptions>(options =>
                options.Secure = CookieSecurePolicy.SameAsRequest);
            services.AddScoped<IReCaptchaValidator, AlwaysPassReCaptchaValidator>();
            services.AddSingleton<IFacebookService, NullFacebookService>();
            services.AddSingleton<IGoogleApiHelper, NullGoogleApiHelper>();
        });
    }
}

public class IntegrationTests : IClassFixture<GuildfordBsacWebApplicationFactory>
{
    private readonly GuildfordBsacWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IntegrationTests(GuildfordBsacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/About")]
    [InlineData("/Home/PrivacyPolicy")]
    [InlineData("/Home/CodeOfConduct")]
    [InlineData("/Home/ContactUs")]
    [InlineData("/Home/Faqs")]
    [InlineData("/Training")]
    [InlineData("/YearPlanner")]
    public async Task Get_ReturnsOk(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LegacyYearPlannerUrl_RedirectsTo_YearPlanner()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/home/YearPlanner");
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.EndsWith("/YearPlanner", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task ContactPost_HoneypotFilled_ReturnsFalseSuccess()
    {
        var token = await GetAntiForgeryTokenAsync();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Test User",
            ["Emaily"] = "test@example.com",
            ["Subject"] = "Test subject",
            ["Message"] = "Test message body",
            ["Emailx"] = "bot@spam.com"
        });

        var response = await _client.PostAsync("/Home/Contact", form);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task ContactPost_MissingRequiredFields_ReturnsValidationErrors()
    {
        var token = await GetAntiForgeryTokenAsync();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });

        var response = await _client.PostAsync("/Home/Contact", form);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("errors").GetArrayLength() > 0);
    }

    [Fact]
    public void AppSettings_CalendarIds_AreConfigured()
    {
        var settings = _factory.Services.GetRequiredService<IOptions<AppSettings>>();
        Assert.NotEmpty(settings.Value.CalendarIds);
    }

    private async Task<string> GetAntiForgeryTokenAsync()
    {
        var response = await _client.GetAsync("/Home/ContactUs");
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        return match.Groups[1].Value;
    }
}
