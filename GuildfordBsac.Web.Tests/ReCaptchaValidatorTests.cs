using GuildfordBsac.Web.Common;
using GuildfordBsac.Web.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Net.Http.Json;

namespace GuildfordBsac.Web.Tests;

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_response);
}

internal class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;
    public FakeHttpClientFactory(HttpResponseMessage response)
        => _client = new HttpClient(new FakeHttpMessageHandler(response));
    public HttpClient CreateClient(string name) => _client;
}

internal class ThrowingHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
        => throw new InvalidOperationException("HTTP client should not be used in this test");
}

public class ReCaptchaValidatorTests
{
    private static IOptions<AppSettings> DefaultSettings() => Options.Create(new AppSettings
    {
        RecaptchaSiteKey = "test-site-key",
        RecaptchaApiKey = "test-api-key",
        ServiceAccount = new ServiceAccountSettings { ClientEmail = "svc@test-project.iam.gserviceaccount.com" }
    });

    private static ReCaptchaValidator MakeValidator(HttpResponseMessage apiResponse)
        => new ReCaptchaValidator(DefaultSettings(), new FakeHttpClientFactory(apiResponse), NullLogger<ReCaptchaValidator>.Instance);

    private static HttpContext MakeContext(string token)
    {
        var context = new DefaultHttpContext();
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["g-recaptcha-response"] = token
        });
        return context;
    }

    private static HttpResponseMessage OkAssessment(bool valid, float score) =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                tokenProperties = new { valid, hostname = "example.com", invalidReason = "" },
                riskAnalysis = new { score }
            })
        };

    [Fact]
    public async Task EmptyToken_ReturnsFalse_WithoutCallingApi()
    {
        var validator = new ReCaptchaValidator(DefaultSettings(), new ThrowingHttpClientFactory(), NullLogger<ReCaptchaValidator>.Instance);
        var result = await validator.ValidateAsync(MakeContext(""));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ApiErrorResponse_ReturnsFalse()
    {
        var validator = MakeValidator(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad request")
        });
        var result = await validator.ValidateAsync(MakeContext("any-token"));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task TokenInvalid_ReturnsFalse()
    {
        var validator = MakeValidator(OkAssessment(valid: false, score: 0.9f));
        var result = await validator.ValidateAsync(MakeContext("any-token"));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidToken_ScoreBelowThreshold_ReturnsFalse()
    {
        var validator = MakeValidator(OkAssessment(valid: true, score: 0.3f));
        var result = await validator.ValidateAsync(MakeContext("any-token"));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidToken_ScoreAtThreshold_ReturnsTrue()
    {
        // score < 0.5 fails; 0.5 itself passes (condition is strict less-than)
        var validator = MakeValidator(OkAssessment(valid: true, score: 0.5f));
        var result = await validator.ValidateAsync(MakeContext("any-token"));
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ValidToken_ScoreAboveThreshold_ReturnsTrue()
    {
        var validator = MakeValidator(OkAssessment(valid: true, score: 0.9f));
        var result = await validator.ValidateAsync(MakeContext("any-token"));
        Assert.True(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("name@nodot")]
    public void Constructor_InvalidServiceAccountEmail_Throws(string badEmail)
    {
        var settings = Options.Create(new AppSettings
        {
            ServiceAccount = new ServiceAccountSettings { ClientEmail = badEmail }
        });
        Assert.Throws<ArgumentException>(() =>
            new ReCaptchaValidator(settings, new ThrowingHttpClientFactory(), NullLogger<ReCaptchaValidator>.Instance));
    }
}
