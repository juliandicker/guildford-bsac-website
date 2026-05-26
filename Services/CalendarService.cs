namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Configuration;
    using GuildfordBsac.Web.Models;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class CalendarService : ICalendarService, IAsyncDisposable
    {
        private static readonly TimeSpan FreshCacheDuration = TimeSpan.FromHours(1);
        private static readonly TimeSpan StaleCacheDuration = TimeSpan.FromHours(24);

        private readonly IGoogleCalendarClient _calendarClient;
        private readonly IMemoryCache _cache;
        private readonly string[] _calendarIds;
        private readonly ILogger<CalendarService> _logger;
        private readonly CachedFetcher<IReadOnlyList<Calendar>> _fetcher;

        public CalendarService(IGoogleCalendarClient calendarClient, IMemoryCache cache, IOptions<AppSettings> settings, ILogger<CalendarService> logger)
        {
            _calendarClient = calendarClient;
            _cache = cache;
            _calendarIds = settings.Value.CalendarIds;
            _logger = logger;
            _fetcher = new CachedFetcher<IReadOnlyList<Calendar>>(cache);
        }

        public async Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, CancellationToken cancellationToken = default)
        {
            var freshKey = $"Calendar_{year}";
            var staleKey = $"Calendar_{year}_stale";

            return await _fetcher.GetOrFetchAsync(freshKey, async ct =>
            {
                try
                {
                    var result = await _calendarClient.GetCalendarsAsync(year, _calendarIds, ct);
                    _cache.Set(staleKey, result, StaleCacheDuration);
                    return (result, FreshCacheDuration);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Google Calendar API failed for year {Year}; attempting stale fallback", year);
                    if (_cache.TryGetValue(staleKey, out IReadOnlyList<Calendar>? stale) && stale != null)
                    {
                        _logger.LogWarning("Returning stale calendar data for year {Year}", year);
                        return (stale, TimeSpan.Zero);
                    }
                    _logger.LogWarning("No stale calendar data available for year {Year}; returning empty", year);
                    return (Array.Empty<Calendar>(), TimeSpan.Zero);
                }
            }, cancellationToken) ?? Array.Empty<Calendar>();
        }

        public ValueTask DisposeAsync() => _fetcher.DisposeAsync();
    }
}
