namespace GuildfordBsac.Web.Common
{
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Properties;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Calendar.v3;
    using Google.Apis.Gmail.v1;
    using Google.Apis.Services;
    using MimeKit;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Net.Mail;

    public interface IGoogleApiHelper
    {
        List<Calendar> GetCalendars(int year, string[] calendarIds);
        bool SendMessage(string name, string email, string subject, string message);
    }

    public class GoogleApiHelper : IGoogleApiHelper
    {
        private static string ApplicationName = "GBSAC";
        private const string userAccountEmail = "gbsacadmin@guildford-bsac.com";
        private readonly string _clientEmail;
        private readonly string _privateKey;
        private readonly string _contactEmail;
        private readonly string _contactEmailBcc;
        private ServiceAccountCredential? _credential;
        private readonly ILogger<GoogleApiHelper> _logger;

        public GoogleApiHelper(IOptions<AppSettings> settings, ILogger<GoogleApiHelper> logger)
        {
            _logger = logger;
            _clientEmail = settings.Value.ServiceAccount.ClientEmail;
            // Env vars store \n as literal backslash-n; normalize to actual newlines
            _privateKey = settings.Value.ServiceAccount.PrivateKey.Replace("\\n", "\n");
            _contactEmail = settings.Value.ContactEmail;
            _contactEmailBcc = settings.Value.ContactEmailBcc;
        }

        private ServiceAccountCredential Credential => _credential ??= CreateServiceAccountCredential(new[] {
            CalendarService.Scope.CalendarReadonly,
            GmailService.Scope.GmailSend
        });

        public List<Calendar> GetCalendars(int year, string[] calendarIds)
        {
            try
            {
                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Credential,
                    ApplicationName = ApplicationName,
                });

                CalendarAdapter adapter = new CalendarAdapter();

                foreach (var calId in calendarIds)
                {
                    CalendarListResource.GetRequest getRequest = service.CalendarList.Get(calId);

                    EventsResource.ListRequest request = service.Events.List(calId);
                    var timeMin = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    request.TimeMinDateTimeOffset = timeMin;
                    request.TimeMaxDateTimeOffset = timeMin.AddYears(1).AddSeconds(-1);
                    request.ShowDeleted = false;
                    request.SingleEvents = true;
                    request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                    adapter.AddCalendar(getRequest.Execute(), request.Execute());
                }

                return adapter.GetCalendars();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Google Calendar data for year {Year}", year);
                throw;
            }
        }

        public bool SendMessage(string name, string email, string subject, string message)
        {
            try
            {
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Credential,
                    ApplicationName = ApplicationName,
                });

                var mimeMessage = MimeMessage.CreateFromMailMessage(CreateMailMessage(name, email, subject, message));

                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = Encode(mimeMessage.ToString())
                };

                service.Users.Messages.Send(gmailMessage, userAccountEmail).Execute();

                return true;
            }
            catch (Exception ex)
            {
                // Sanitize user-supplied values: strip newlines (log-forging) and redact email local-part (PII)
                var safeName = name.ReplaceLineEndings(" ");
                var safeEmail = email.Contains('@')
                    ? $"[redacted]@{email[(email.IndexOf('@') + 1)..].ReplaceLineEndings(" ")}"
                    : "[redacted]";
                _logger.LogError(ex, "Failed to send Gmail message from {Name} <{Email}>", safeName, safeEmail);
                throw;
            }
        }

        private ServiceAccountCredential CreateServiceAccountCredential(IEnumerable<string> scopes)
        {
            return new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(_clientEmail)
                {
                    Scopes = scopes,
                    User = userAccountEmail
                }.FromPrivateKey(_privateKey));
        }

        private MailMessage CreateMailMessage(string name, string email, string subject, string message)
        {
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(userAccountEmail);
            mailMessage.To.Add(_contactEmail);
            mailMessage.ReplyToList.Add(email);

            if (_contactEmailBcc.Length > 0)
            {
                foreach (var a in _contactEmailBcc.Split(','))
                {
                    mailMessage.Bcc.Add(a.Trim());
                }
            }

            mailMessage.Subject = subject;

            mailMessage.Body = new string('*', 20)
                + Environment.NewLine
                + "This email was generated on www.guildford-bsac.com"
                + Environment.NewLine
                + "Reply to: " + name + " (" + email + ")"
                + Environment.NewLine
                + new string('*', 20)
                + Environment.NewLine
                + message;

            mailMessage.IsBodyHtml = false;

            return mailMessage;
        }

        private static string Encode(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
    }
}
