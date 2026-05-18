namespace GuildfordBsac.Web.Common
{
    using GuildfordBsac.Web.Models;
    using System;
    using System.Collections.Generic;

    public class CalendarAdapter
    {
        private List<Calendar> _calendars = new List<Calendar>();

        public List<Calendar> GetCalendars()
        {
            return _calendars;
        }

        public void AddCalendar(Google.Apis.Calendar.v3.Data.CalendarListEntry gcal, Google.Apis.Calendar.v3.Data.Events gevents, bool splitEventsOverMonths = true)
        {
            var cal = new Calendar();
            cal.Events = new List<Event>();
            cal.Name = gcal.Summary;
            cal.BackgroundColor = gcal.BackgroundColor;

            if (gevents.Items != null && gevents.Items.Count > 0)
            {
                foreach (var gevent in gevents.Items)
                {
                    var start = gevent.Start.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(gevent.Start.Date);
                    var end = gevent.End.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(gevent.End.Date);
                    var endMinusSecond = end.AddSeconds(-1);
                    var eventIsInSameMonth = start.Month >= endMinusSecond.Month;

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

        private static void AddEvent(Calendar cal, DateTime start, DateTime end, Google.Apis.Calendar.v3.Data.Event gevent)
        {
            var evt = new Event
            {
                StartDate = start,
                EndDate = end,
                Summary = gevent.Summary,
                Description = gevent.Description
            };

            evt.Duration = Math.Max((evt.EndDate - evt.StartDate).TotalDays, 1);

            cal.Events.Add(evt);
        }
    }
}