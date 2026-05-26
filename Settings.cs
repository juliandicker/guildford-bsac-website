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
}
