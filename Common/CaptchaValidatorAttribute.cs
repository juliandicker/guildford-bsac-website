namespace GuildfordBsac.Web.Common
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using GuildfordBsac.Web.Configuration;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public interface IReCaptchaValidator
    {
        Task<ReCaptchaResponse> ValidateAsync(HttpContext context, CancellationToken cancellationToken = default);
    }

    public class ReCaptchaValidator : IReCaptchaValidator
    {
        private const float MinimumAcceptableScore = 0.5f;

        private readonly string _siteKey;
        private readonly string _apiKey;
        private readonly string _projectId;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReCaptchaValidator> _logger;

        public ReCaptchaValidator(IOptions<AppSettings> settings, IHttpClientFactory httpClientFactory, ILogger<ReCaptchaValidator> logger)
        {
            var appSettings = settings.Value;
            _siteKey = appSettings.RecaptchaSiteKey;
            _apiKey = appSettings.RecaptchaApiKey;
            _projectId = ExtractProjectId(appSettings.ServiceAccount.ClientEmail);
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ReCaptchaResponse> ValidateAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            var token = context.Request.Form["g-recaptcha-response"].ToString();
            if (string.IsNullOrEmpty(token))
                return new ReCaptchaResponse { Success = false };

            try
            {
                var client = _httpClientFactory.CreateClient("recaptcha");
                var requestBody = new
                {
                    @event = new { token, siteKey = _siteKey }
                };

                var url = $"https://recaptchaenterprise.googleapis.com/v1/projects/{_projectId}/assessments?key={_apiKey}";
                var response = await client.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("reCAPTCHA Enterprise API returned {Status}: {Body}", (int)response.StatusCode, body);
                    return new ReCaptchaResponse { Success = false };
                }

                var assessment = await response.Content.ReadFromJsonAsync<EnterpriseAssessment>(cancellationToken: cancellationToken)
                    ?? new EnterpriseAssessment();

                var valid = assessment.TokenProperties?.Valid ?? false;
                var score = assessment.RiskAnalysis?.Score ?? 0f;

                if (!valid)
                    _logger.LogWarning("reCAPTCHA token invalid: hostname={Hostname}, reason={Reason}",
                        assessment.TokenProperties?.Hostname ?? "",
                        assessment.TokenProperties?.InvalidReason ?? "");
                else if (score < MinimumAcceptableScore)
                {
                    _logger.LogWarning("reCAPTCHA score too low: {Score}", score);
                    valid = false;
                }

                return new ReCaptchaResponse { Success = valid };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "reCAPTCHA validation request failed");
                return new ReCaptchaResponse { Success = false };
            }
        }

        private static string ExtractProjectId(string serviceAccountEmail)
        {
            // Service account emails follow the format: name@project-id.iam.gserviceaccount.com
            var atIndex = serviceAccountEmail.IndexOf('@');
            var dotIndex = atIndex >= 0 ? serviceAccountEmail.IndexOf('.', atIndex + 1) : -1;
            if (atIndex < 0 || dotIndex <= atIndex)
                throw new ArgumentException(
                    $"Service account email '{serviceAccountEmail}' does not follow the expected format 'name@project-id.iam.gserviceaccount.com'.",
                    nameof(serviceAccountEmail));
            return serviceAccountEmail[(atIndex + 1)..dotIndex];
        }
    }

    public class ReCaptchaResponse
    {
        public bool Success { get; set; }
    }

    internal class EnterpriseAssessment
    {
        [JsonPropertyName("tokenProperties")]
        public EnterpriseTokenProperties? TokenProperties { get; set; }

        [JsonPropertyName("riskAnalysis")]
        public EnterpriseRiskAnalysis? RiskAnalysis { get; set; }
    }

    internal class EnterpriseTokenProperties
    {
        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = "";

        [JsonPropertyName("invalidReason")]
        public string InvalidReason { get; set; } = "";
    }

    internal class EnterpriseRiskAnalysis
    {
        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}
