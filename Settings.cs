namespace GuildfordBsac.Web.Properties
{
    public class ServiceAccountSettings
    {
        public string ClientEmail { get; set; } = "";
        public string PrivateKey { get; set; } = "";
        public string UserEmail { get; set; } = "";
    }

    public class AppSettings
    {
        public string RecaptchaSiteKey { get; set; } = "";
        public string RecaptchaApiKey { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public string ContactEmailBcc { get; set; } = "";
        public string WkHtmlPdfCustomSwitches { get; set; } = "";
        public string[] CalendarIds { get; set; } = Array.Empty<string>();
        public ServiceAccountSettings ServiceAccount { get; set; } = new();
    }
}
