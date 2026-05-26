namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IContactFormService
    {
        // Validates reCAPTCHA, honeypot, and sends the email. Returns error messages (empty = success).
        // Caller is responsible for ModelState field validation before calling this.
        Task<IReadOnlyList<string>> SubmitAsync(ContactViewModel model, HttpContext httpContext, CancellationToken cancellationToken = default);
    }

    public class ContactFormService : IContactFormService
    {
        private const string TechnicalErrorMessage = "There has been a technical error sending this message. Please contact by telephone.";

        private readonly IReCaptchaValidator _captcha;
        private readonly IEmailService _email;
        private readonly ILogger<ContactFormService> _logger;

        public ContactFormService(IReCaptchaValidator captcha, IEmailService email, ILogger<ContactFormService> logger)
        {
            _captcha = captcha;
            _email = email;
            _logger = logger;
        }

        public async Task<IReadOnlyList<string>> SubmitAsync(ContactViewModel model, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var reCaptchaResponse = await _captcha.ValidateAsync(httpContext, cancellationToken);
            if (!reCaptchaResponse.Success)
                return new[] { "You failed to pass the captcha test" };

            if (!string.IsNullOrEmpty(model.Emailx))
                return new[] { TechnicalErrorMessage };

            if (!await _email.SendContactFormEmailAsync(model.Name!, model.Emaily!, model.Subject!, model.Message!, cancellationToken))
                return new[] { TechnicalErrorMessage };

            return Array.Empty<string>();
        }
    }
}
