using GuildfordBsac.Web.Configuration;
using GuildfordBsac.Web.Models;
using GuildfordBsac.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GuildfordBsac.Web.Tests;

internal class CountingGoogleCalendarClient : IGoogleCalendarClient
{
    private int _callCount;
    public int CallCount => _callCount;

    public Task<IReadOnlyList<CalendarModel>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromResult<IReadOnlyList<CalendarModel>>(new List<CalendarModel>());
    }
}

public class CalendarServiceTests
{
    private static (CalendarService service, CountingGoogleCalendarClient client) MakeService(params string[] calendarIds)
    {
        var calendarClient = new CountingGoogleCalendarClient();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var settings = Options.Create(new AppSettings { CalendarIds = calendarIds });
        return (new CalendarService(calendarClient, cache, settings, NullLogger<CalendarService>.Instance), calendarClient);
    }

    [Fact]
    public async Task GetCalendarsAsync_SecondCallSameYear_UsesCachedResult()
    {
        var (service, client) = MakeService("cal1");

        await service.GetCalendarsAsync(2025);
        await service.GetCalendarsAsync(2025);

        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task GetCalendarsAsync_DifferentYears_CallsApiForEachYear()
    {
        var (service, client) = MakeService("cal1");

        await service.GetCalendarsAsync(2025);
        await service.GetCalendarsAsync(2026);

        Assert.Equal(2, client.CallCount);
    }

    [Fact]
    public async Task GetCalendarsAsync_ReturnsResultFromApi()
    {
        var (service, _) = MakeService("cal1");

        var result = await service.GetCalendarsAsync(2025);

        Assert.NotNull(result);
    }
}
