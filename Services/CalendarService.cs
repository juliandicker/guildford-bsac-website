namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Properties;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class CalendarService : ICalendarService
    {
        private readonly IGoogleApiHelper _googleApi;
        private readonly IMemoryCache _cache;
        private readonly string[] _calendarIds;
        private readonly SemaphoreSlim _fetchLock = new SemaphoreSlim(1, 1);

        public CalendarService(IGoogleApiHelper googleApi, IMemoryCache cache, IOptions<AppSettings> settings)
        {
            _googleApi = googleApi;
            _cache = cache;
            _calendarIds = settings.Value.CalendarIds;
        }

        public async Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"Calendar_{year}";

            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<Calendar>? cached))
                return cached!;

            await _fetchLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring the lock — a concurrent caller may have populated the cache
                if (_cache.TryGetValue(cacheKey, out cached))
                    return cached!;

                var result = await _googleApi.GetCalendarsAsync(year, _calendarIds, cancellationToken);
                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
                return result;
            }
            finally
            {
                _fetchLock.Release();
            }
        }
    }
}
