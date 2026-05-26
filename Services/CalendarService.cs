namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Configuration;
    using GuildfordBsac.Web.Models;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class CalendarService : ICalendarService, IDisposable
    {
        private readonly IGoogleCalendarClient _calendarClient;
        private readonly IMemoryCache _cache;
        private readonly string[] _calendarIds;
        private readonly SemaphoreSlim _fetchLock = new SemaphoreSlim(1, 1);

        public CalendarService(IGoogleCalendarClient calendarClient, IMemoryCache cache, IOptions<AppSettings> settings)
        {
            _calendarClient = calendarClient;
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

                var result = await _calendarClient.GetCalendarsAsync(year, _calendarIds, cancellationToken);
                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
                return result;
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        public void Dispose() => _fetchLock.Dispose();
    }
}
