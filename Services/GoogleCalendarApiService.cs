namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Configuration;
    using GuildfordBsac.Web.Models;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Services;
    using GoogleCalendarService = Google.Apis.Calendar.v3.CalendarService;
    using Google.Apis.Calendar.v3;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class GoogleCalendarApiService : IGoogleCalendarClient
    {
        private const string ApplicationName = "GBSAC";
        private const int ApiTimeoutSeconds = 20;

        private readonly string[] _calendarScopes = new[] { GoogleCalendarService.Scope.CalendarReadonly };
        private readonly ServiceAccountSettings _sa;
        private readonly string _privateKey;
        private readonly Lazy<GoogleCalendarService> _service;
        private readonly ILogger<GoogleCalendarApiService> _logger;

        public GoogleCalendarApiService(IOptions<AppSettings> settings, ILogger<GoogleCalendarApiService> logger)
        {
            _logger = logger;
            _sa = settings.Value.ServiceAccount;
            // Env vars store \n as literal backslash-n; normalize to actual newlines
            _privateKey = _sa.PrivateKey.Replace("\\n", "\n");
            _service = new Lazy<GoogleCalendarService>(() =>
            {
                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(_sa.ClientEmail)
                    {
                        Scopes = _calendarScopes,
                        User = _sa.UserEmail
                    }.FromPrivateKey(_privateKey));
                return new GoogleCalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });
            });
        }

        public async Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(ApiTimeoutSeconds));
            var token = cts.Token;

            var service = _service.Value;
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

                var metaTask   = service.CalendarList.Get(calId).ExecuteAsync(token);
                var eventsTask = eventsRequest.ExecuteAsync(token);

                return (Meta: await metaTask, Events: await eventsTask);
            });

            var results = await Task.WhenAll(tasks);

            var adapter = new CalendarAdapter(_logger);
            foreach (var (meta, events) in results)
                adapter.AddCalendar(meta, events);

            return adapter.GetCalendars();
        }
    }
}
