namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Properties;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;

    public class CalendarService : ICalendarService
    {
        private readonly IGoogleApiHelper _googleApi;
        private readonly string[] _calendarIds;

        public CalendarService(IGoogleApiHelper googleApi, IOptions<AppSettings> settings)
        {
            _googleApi = googleApi;
            _calendarIds = settings.Value.CalendarIds;
        }

        public List<Calendar> GetCalendars(int year)
        {
            return _googleApi.GetCalendars(year, _calendarIds);
        }
    }
}
