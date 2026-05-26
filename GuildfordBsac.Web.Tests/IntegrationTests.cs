using GuildfordBsac.Web.Common;
using GuildfordBsac.Web.Configuration;
using GuildfordBsac.Web.Models;
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

internal class NullGoogleCalendarClient : IGoogleCalendarClient
{
    public Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Calendar>>(new List<Calendar>());
}

internal class SuccessEmailService : IEmailService
{
    public Task<bool> SendContactFormEmailAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

internal class FailureEmailService : IEmailService
{
    public Task<bool> SendContactFormEmailAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

public class GuildfordBsacWebApplicationFactory : WebApplicationFactory<Program>
{
    private IEmailService? _emailService;

    public GuildfordBsacWebApplicationFactory() { }

    public static GuildfordBsacWebApplicationFactory WithEmailService(IEmailService emailService)
        => new() { _emailService = emailService };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Navigate from bin/Release|Debug/net8.0/ up to the repo root where App_Data lives
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        builder.UseContentRoot(webRoot);
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // CookieSecurePolicy.Always marks cookies as Secure; the test HttpClient uses
            // plain HTTP so override both the global policy and antiforgery-specific policy.
            services.Configure<CookiePolicyOptions>(options =>
                options.Secure = CookieSecurePolicy.SameAsRequest);
            services.AddAntiforgery(options =>
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
            services.AddScoped<IReCaptchaValidator, AlwaysPassReCaptchaValidator>();
            services.AddSingleton<IFacebookService, NullFacebookService>();
            services.AddSingleton<IGoogleCalendarClient, NullGoogleCalendarClient>();
            services.AddSingleton<IEmailService>(_emailService ?? new SuccessEmailService());
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
    public async Task ContactPost_SendEmailReturnsFalse_ReturnsErrorResponse()
    {
        var failFactory = GuildfordBsacWebApplicationFactory.WithEmailService(new FailureEmailService());
        var client = failFactory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client);
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Test User",
            ["Emaily"] = "test@example.com",
            ["Subject"] = "Test subject",
            ["Message"] = "Test message body"
        });

        var response = await client.PostAsync("/Home/Contact", form);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("errors").GetArrayLength() > 0);
    }

    [Fact]
    public async Task ContactPost_ValidSubmission_ReturnsSuccess()
    {
        var token = await GetAntiForgeryTokenAsync();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Test User",
            ["Emaily"] = "test@example.com",
            ["Subject"] = "Test subject",
            ["Message"] = "Test message body that is long enough"
        });

        var response = await _client.PostAsync("/Home/Contact", form);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(0, doc.RootElement.GetProperty("errors").GetArrayLength());
    }

    [Theory]
    [InlineData("/YearPlanner?year=1900")]  // far past — clamped
    [InlineData("/YearPlanner?year=2100")]  // far future — clamped
    [InlineData("/YearPlanner?year=abc")]   // non-integer — default year
    public async Task YearPlanner_OutOfRangeOrInvalidYear_ReturnsOk(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task YearPlanner_NoYearParam_ReturnsOk()
    {
        var response = await _client.GetAsync("/YearPlanner");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task YearPlanner_AgendaFalse_ReturnsOk()
    {
        var response = await _client.GetAsync("/YearPlanner?agenda=false");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task YearPlanner_Pdf_ReturnsResult()
    {
        var response = await _client.GetAsync("/YearPlanner/Pdf");
        // Rotativa requires a real wkhtmltopdf binary; in CI this returns an error result,
        // but the endpoint must not throw an unhandled exception (5xx).
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public void AppSettings_CalendarIds_AreConfigured()
    {
        var settings = _factory.Services.GetRequiredService<IOptions<AppSettings>>();
        Assert.NotEmpty(settings.Value.CalendarIds);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> GetAntiForgeryTokenAsync() => await GetAntiForgeryTokenAsync(_client);

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/Home/ContactUs");
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        return match.Groups[1].Value;
    }
}
