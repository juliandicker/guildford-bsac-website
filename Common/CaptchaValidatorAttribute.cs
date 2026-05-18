namespace GuildfordBsac.Web.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;

    public class ReCaptcha
    {
        public static async Task<ReCaptchaResponse> ValidateAsync(HttpContext context, string secret)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                var form = new Dictionary<string, string>
                {
                    ["secret"] = secret,
                    ["response"] = context.Request.Form["g-recaptcha-response"].ToString(),
                    ["remoteip"] = context.Connection.RemoteIpAddress?.ToString() ?? ""
                };

                var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(form));
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<ReCaptchaResponse>(jsonString)!;
            }
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
