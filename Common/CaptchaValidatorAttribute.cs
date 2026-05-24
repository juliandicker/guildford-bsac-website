namespace GuildfordBsac.Web.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using GuildfordBsac.Web.Properties;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    public interface IReCaptchaValidator
    {
        Task<ReCaptchaResponse> ValidateAsync(HttpContext context);
    }

    public class ReCaptchaValidator : IReCaptchaValidator
    {
        private readonly string _secret;

        public ReCaptchaValidator(IOptions<AppSettings> settings)
        {
            _secret = settings.Value.RecaptchaSecret;
        }

        public async Task<ReCaptchaResponse> ValidateAsync(HttpContext context)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var form = new Dictionary<string, string>
            {
                ["secret"] = _secret,
                ["response"] = context.Request.Form["g-recaptcha-response"].ToString(),
                ["remoteip"] = context.Connection.RemoteIpAddress?.ToString() ?? ""
            };
            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(form));
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ReCaptchaResponse>(jsonString)!;
        }
    }

    public class ReCaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("challenge_ts")]
        public DateTime ChallengeTimeStamp { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; } = "";

        [JsonProperty("error-codes")]
        public List<string> ErrorCodes { get; set; } = new();
    }
}
