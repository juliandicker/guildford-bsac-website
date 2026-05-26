namespace GuildfordBsac.Web.Models
{
    using System;
    using System.Collections.Generic;

    public class CalendarModel
    {
        public string Name { get; set; } = string.Empty;
        public IList<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
        public string BackgroundColor { get; internal set; } = string.Empty;
    }

    public class CalendarEvent
    {
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double Duration { get; set; }
    }
}
