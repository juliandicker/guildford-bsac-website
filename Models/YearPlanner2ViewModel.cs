namespace GuildfordBsac.Web.Models
{
    using System;
    using System.Collections.Generic;

    public class YearPlanner2ViewModel
    {
        public int Year { get; set; }
        public bool ShowAgenda { get; set; }
        public bool IsPdf { get; set; }
        public string CalendarData { get; set; } = string.Empty;
    }

    public class Calendar
    {
        public string Name { get; set; } = string.Empty;
        public IList<Event> Events { get; set; } = new List<Event>();
        public string BackgroundColor { get; internal set; } = string.Empty;
    }

    public class Event
    {
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Double Duration { get; set; }
    }
}
