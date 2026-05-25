namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System.Collections.Generic;

    public interface ICalendarService
    {
        List<Calendar> GetCalendars(int year);
    }
}
