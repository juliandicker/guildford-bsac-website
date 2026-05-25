namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;

    public class EmailService : IEmailService
    {
        private readonly IGoogleApiHelper _googleApi;

        public EmailService(IGoogleApiHelper googleApi)
        {
            _googleApi = googleApi;
        }

        public bool SendContactEmail(string name, string email, string subject, string message)
        {
            return _googleApi.SendMessage(name, email, subject, message);
        }
    }
}
