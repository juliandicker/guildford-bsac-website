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
    using System.Threading;
    using System.Threading.Tasks;

    public interface IGoogleApiHelper
    {
        Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default);
        Task<bool> SendMessageAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default);
    }

    public class GoogleApiHelper : IGoogleApiHelper
    {
        private const string ApplicationName = "GBSAC";
        private readonly string _clientEmail;
        private readonly string _privateKey;
        private readonly string _userAccountEmail;
        private readonly string _contactEmail;
        private readonly string _contactEmailBcc;
        private readonly Lazy<ServiceAccountCredential> _credential;
        private readonly Lazy<CalendarService> _calendarService;
        private readonly Lazy<GmailService> _gmailService;
        private readonly ILogger<GoogleApiHelper> _logger;

        public GoogleApiHelper(IOptions<AppSettings> settings, ILogger<GoogleApiHelper> logger)
        {
            _logger = logger;
            _clientEmail = settings.Value.ServiceAccount.ClientEmail;
            // Env vars store \n as literal backslash-n; normalize to actual newlines
            _privateKey = settings.Value.ServiceAccount.PrivateKey.Replace("\\n", "\n");
            _userAccountEmail = settings.Value.ServiceAccount.UserEmail;
            _contactEmail = settings.Value.ContactEmail;
            _contactEmailBcc = settings.Value.ContactEmailBcc;
            _credential = new Lazy<ServiceAccountCredential>(() => CreateServiceAccountCredential(
                new[] { CalendarService.Scope.CalendarReadonly, GmailService.Scope.GmailSend }));
            _calendarService = new Lazy<CalendarService>(() => new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Credential,
                ApplicationName = ApplicationName,
            }));
            _gmailService = new Lazy<GmailService>(() => new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Credential,
                ApplicationName = ApplicationName,
            }));
        }

        private ServiceAccountCredential Credential => _credential.Value;

        public async Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var service = _calendarService.Value;

                var timeMin = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var timeMax = timeMin.AddYears(1).AddSeconds(-1);

                var tasks = calendarIds.Select(async calId =>
                {
                    var eventsRequest = service.Events.List(calId);
                    eventsRequest.TimeMinDateTimeOffset = timeMin;
                    eventsRequest.TimeMaxDateTimeOffset = timeMax;
                    eventsRequest.ShowDeleted = false;
                    eventsRequest.SingleEvents = true;
                    eventsRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                    // Fire both requests concurrently — they are independent
                    var metaTask   = service.CalendarList.Get(calId).ExecuteAsync(cancellationToken);
                    var eventsTask = eventsRequest.ExecuteAsync(cancellationToken);

                    return (
                        Meta: await metaTask,
                        Events: await eventsTask
                    );
                });

                var results = await Task.WhenAll(tasks);

                var adapter = new CalendarAdapter();
                foreach (var (meta, events) in results)
                    adapter.AddCalendar(meta, events);

                return adapter.GetCalendars();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Google Calendar data for year {Year}", year);
                throw;
            }
        }

        public async Task<bool> SendMessageAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var service = _gmailService.Value;

                using var mailMessage = CreateMailMessage(name, email, subject, message);
                var mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);

                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = Encode(mimeMessage.ToString())
                };

                await service.Users.Messages.Send(gmailMessage, _userAccountEmail).ExecuteAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Gmail message");
                throw;
            }
        }

        private ServiceAccountCredential CreateServiceAccountCredential(IEnumerable<string> scopes)
        {
            return new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(_clientEmail)
                {
                    Scopes = scopes,
                    User = _userAccountEmail
                }.FromPrivateKey(_privateKey));
        }

        private MailMessage CreateMailMessage(string name, string email, string subject, string message)
        {
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_userAccountEmail);
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
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
