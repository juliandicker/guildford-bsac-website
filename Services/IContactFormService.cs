namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using Microsoft.AspNetCore.Http;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IContactFormService
    {
        // Validates reCAPTCHA, honeypot, and sends the email. Returns error messages (empty = success).
        // Caller is responsible for ModelState field validation before calling this.
        Task<IReadOnlyList<string>> SubmitAsync(ContactViewModel model, HttpContext httpContext, CancellationToken cancellationToken = default);
    }
}
