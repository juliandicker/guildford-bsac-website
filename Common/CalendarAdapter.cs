namespace GuildfordBsac.Web.Common
{
    using Ganss.Xss;
    using GuildfordBsac.Web.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class CalendarAdapter
    {
        private static readonly HtmlSanitizer _sanitizer = SharedHtmlSanitizer.Instance;
        private static readonly Regex _colorPattern = new Regex(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        private readonly ILogger _logger;
        private readonly List<CalendarModel> _calendars = new List<CalendarModel>();

        public CalendarAdapter(ILogger? logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public IReadOnlyList<CalendarModel> GetCalendars() => _calendars.AsReadOnly();

        public void AddCalendar(Google.Apis.Calendar.v3.Data.CalendarListEntry gcal, Google.Apis.Calendar.v3.Data.Events gevents, bool splitEventsOverMonths = true)
        {
            // Validate BackgroundColor to prevent CSS injection via calendar data
            var bgColor = gcal.BackgroundColor ?? "#000000";
            if (!_colorPattern.IsMatch(bgColor))
                bgColor = "#000000";

            var cal = new CalendarModel
            {
                Events = new List<CalendarEvent>(),
                Name = gcal.Summary,
                BackgroundColor = bgColor
            };

            if (gevents.Items != null && gevents.Items.Count > 0)
            {
                foreach (var gevent in gevents.Items)
                {
                    if (gevent.Start == null || gevent.End == null)
                    {
                        _logger.LogError("Skipping calendar event with null Start/End: Id={EventId} Summary={Summary}", gevent.Id, gevent.Summary);
                        continue;
                    }

                    DateTime start, end;
                    if (gevent.Start.DateTimeDateTimeOffset.HasValue)
                    {
                        start = gevent.Start.DateTimeDateTimeOffset.Value.DateTime;
                    }
                    else if (!DateTime.TryParse(gevent.Start.Date, out start))
                    {
                        _logger.LogError("Skipping calendar event with unparseable start date: Id={EventId} Date={Date}", gevent.Id, gevent.Start.Date);
                        continue;
                    }

                    if (gevent.End.DateTimeDateTimeOffset.HasValue)
                    {
                        end = gevent.End.DateTimeDateTimeOffset.Value.DateTime;
                    }
                    else if (!DateTime.TryParse(gevent.End.Date, out end))
                    {
                        _logger.LogError("Skipping calendar event with unparseable end date: Id={EventId} Date={Date}", gevent.Id, gevent.End.Date);
                        continue;
                    }

                    var endMinusSecond = end.AddSeconds(-1);
                    var eventIsInSameMonth = start.Year == endMinusSecond.Year && start.Month == endMinusSecond.Month;

                    if (!splitEventsOverMonths || eventIsInSameMonth)
                    {
                        AddEvent(cal, start, end, gevent);
                    }
                    else
                    {
                        var startOfSecondMonth = new DateTime(end.Year, end.Month, 1);
                        AddEvent(cal, start, startOfSecondMonth, gevent);
                        AddEvent(cal, startOfSecondMonth, end, gevent);
                    }
                }
            }

            _calendars.Add(cal);
        }

        private static void AddEvent(CalendarModel cal, DateTime start, DateTime end, Google.Apis.Calendar.v3.Data.Event gevent)
        {
            var description = gevent.Description;
            var evt = new CalendarEvent
            {
                StartDate = start,
                EndDate = end,
                Summary = gevent.Summary,
                Description = string.IsNullOrEmpty(description) ? string.Empty : _sanitizer.Sanitize(description)
            };

            evt.Duration = Math.Max((evt.EndDate - evt.StartDate).TotalDays, 1);

            cal.Events.Add(evt);
        }
    }
}
