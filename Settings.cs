namespace GuildfordBsac.Web.Properties
{
    public class ServiceAccountSettings
    {
        public string ClientEmail { get; set; } = "";
        public string PrivateKey { get; set; } = "";
    }

    public class AppSettings
    {
        public string RecaptchaSecret { get; set; } = "";
        public string RecaptchaSiteKey { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public string ContactEmailBcc { get; set; } = "";
        public string WkHtmlPdf_CustomSwitches { get; set; } = "";
        public ServiceAccountSettings ServiceAccount { get; set; } = new();
    }
}
