namespace GuildfordBsac.Web.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEmailService
    {
        Task<bool> SendContactFormEmailAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default);
    }
}
