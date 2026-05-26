namespace GuildfordBsac.Web.Configuration
{
    using System.ComponentModel.DataAnnotations;

    public class ServiceAccountSettings
    {
        [Required]
        public string ClientEmail { get; set; } = "";
        // Injected at runtime via Plesk environment variables — not required at startup
        public string PrivateKey { get; set; } = "";
        [Required]
        public string UserEmail { get; set; } = "";
    }

    public class AppSettings
    {
        [Required]
        public string RecaptchaSiteKey { get; set; } = "";
        // Injected at runtime via Plesk environment variables — not required at startup
        public string RecaptchaApiKey { get; set; } = "";
        // Injected at deploy time from GitHub Actions variable CONTACT_EMAIL — not required at startup
        public string ContactEmail { get; set; } = "";
        public string ContactEmailBcc { get; set; } = "";
        public string WkHtmlPdfCustomSwitches { get; set; } = "";
        public string[] CalendarIds { get; set; } = Array.Empty<string>();
        [Required]
        public ServiceAccountSettings ServiceAccount { get; set; } = new();
    }

    // Bound to the "Facebook" config section. PageAccessToken is injected at deploy time
    // via the Facebook__PageAccessToken GitHub Actions secret → web.config env var.
    public class FacebookSettings
    {
        public string PageAccessToken { get; set; } = "";
        public string PageId { get; set; } = "";
        public string ApiVersion { get; set; } = "v25.0";
        public TimeSpan SuccessCacheDuration { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan ErrorCacheDurationInitial { get; set; } = TimeSpan.FromSeconds(90);
        public TimeSpan ErrorCacheDurationMax { get; set; } = TimeSpan.FromHours(2);
    }
}
