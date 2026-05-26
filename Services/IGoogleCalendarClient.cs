namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IGoogleCalendarClient
    {
        Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default);
    }
}
