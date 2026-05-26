namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Configuration;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Gmail.v1;
    using Google.Apis.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MimeKit;
    using System;
    using System.Collections.Generic;
    using System.Net.Mail;
    using System.Threading;
    using System.Threading.Tasks;

    public class GoogleEmailApiService : IEmailService
    {
        private const string ApplicationName = "GBSAC";
        private const int ApiTimeoutSeconds = 20;

        private readonly string _userAccountEmail;
        private readonly string _contactEmail;
        private readonly string _contactEmailBcc;
        private readonly Lazy<GmailService> _service;
        private readonly ILogger<GoogleEmailApiService> _logger;

        public GoogleEmailApiService(IOptions<AppSettings> settings, ILogger<GoogleEmailApiService> logger)
        {
            _logger = logger;
            var sa = settings.Value.ServiceAccount;
            // Env vars store \n as literal backslash-n; normalize to actual newlines
            var privateKey = sa.PrivateKey.Replace("\\n", "\n");
            _userAccountEmail = sa.UserEmail;
            _contactEmail = settings.Value.ContactEmail;
            _contactEmailBcc = settings.Value.ContactEmailBcc;

            _service = new Lazy<GmailService>(() =>
            {
                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(sa.ClientEmail)
                    {
                        Scopes = new[] { GmailService.Scope.GmailSend },
                        User = sa.UserEmail
                    }.FromPrivateKey(privateKey));
                return new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });
            });
        }

        public async Task<bool> SendContactFormEmailAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(ApiTimeoutSeconds));

            try
            {
                var service = _service.Value;
                using var mailMessage = CreateMailMessage(name, email, subject, message);
                var mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);
                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = Encode(mimeMessage.ToString())
                };
                await service.Users.Messages.Send(gmailMessage, _userAccountEmail).ExecuteAsync(cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Gmail API request timed out after {Seconds}s", ApiTimeoutSeconds);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Gmail message");
                return false;
            }
        }

        private MailMessage CreateMailMessage(string name, string email, string subject, string message)
        {
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_userAccountEmail);
            mailMessage.To.Add(_contactEmail);
            mailMessage.ReplyToList.Add(email);

            if (!string.IsNullOrWhiteSpace(_contactEmailBcc))
            {
                foreach (var a in _contactEmailBcc.Split(','))
                {
                    var addr = a.Trim();
                    if (!string.IsNullOrWhiteSpace(addr))
                        mailMessage.Bcc.Add(addr);
                }
            }

            mailMessage.Subject = subject;
            mailMessage.Body =
                new string('*', 20) + Environment.NewLine +
                "This email was generated on www.guildford-bsac.com" + Environment.NewLine +
                "Reply to: " + name + " (" + email + ")" + Environment.NewLine +
                new string('*', 20) + Environment.NewLine +
                message;
            mailMessage.IsBodyHtml = false;
            return mailMessage;
        }

        private static string Encode(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
