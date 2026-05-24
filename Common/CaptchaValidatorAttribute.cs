namespace GuildfordBsac.Web.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using GuildfordBsac.Web.Properties;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public interface IReCaptchaValidator
    {
        Task<ReCaptchaResponse> ValidateAsync(HttpContext context);
    }

    public class ReCaptchaValidator : IReCaptchaValidator
    {
        private readonly string _secret;
        private readonly IHttpClientFactory _httpClientFactory;

        public ReCaptchaValidator(IOptions<AppSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _secret = settings.Value.RecaptchaSecret;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ReCaptchaResponse> ValidateAsync(HttpContext context)
        {
            var client = _httpClientFactory.CreateClient("recaptcha");
            var form = new Dictionary<string, string>
            {
                ["secret"] = _secret,
                ["response"] = context.Request.Form["g-recaptcha-response"].ToString(),
                ["remoteip"] = context.Connection.RemoteIpAddress?.ToString() ?? ""
            };
            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(form));
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ReCaptchaResponse>(jsonString)!;
        }
    }

    public class ReCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime ChallengeTimeStamp { get; set; }

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = "";

        [JsonPropertyName("error-codes")]
        public List<string> ErrorCodes { get; set; } = new();
    }
}
