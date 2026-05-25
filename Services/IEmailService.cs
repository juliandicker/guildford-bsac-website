namespace GuildfordBsac.Web.Services
{
    public interface IEmailService
    {
        bool SendContactEmail(string name, string email, string subject, string message);
    }
}
