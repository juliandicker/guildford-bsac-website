using GuildfordBsac.Web.Common;
using GuildfordBsac.Web.Models;
using GuildfordBsac.Web.Properties;
using GuildfordBsac.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GuildfordBsac.Web.Tests;

internal class CountingGoogleApiHelper : IGoogleApiHelper
{
    private int _callCount;
    public int CallCount => _callCount;

    public Task<IReadOnlyList<Calendar>> GetCalendarsAsync(int year, string[] calendarIds, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromResult<IReadOnlyList<Calendar>>(new List<Calendar>());
    }

    public Task<bool> SendMessageAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public class CalendarServiceTests
{
    private static (CalendarService service, CountingGoogleApiHelper helper) MakeService(params string[] calendarIds)
    {
        var helper = new CountingGoogleApiHelper();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var settings = Options.Create(new AppSettings { CalendarIds = calendarIds });
        return (new CalendarService(helper, cache, settings), helper);
    }

    [Fact]
    public async Task GetCalendarsAsync_SecondCallSameYear_UsesCachedResult()
    {
        var (service, helper) = MakeService("cal1");

        await service.GetCalendarsAsync(2025);
        await service.GetCalendarsAsync(2025);

        Assert.Equal(1, helper.CallCount);
    }

    [Fact]
    public async Task GetCalendarsAsync_DifferentYears_CallsApiForEachYear()
    {
        var (service, helper) = MakeService("cal1");

        await service.GetCalendarsAsync(2025);
        await service.GetCalendarsAsync(2026);

        Assert.Equal(2, helper.CallCount);
    }

    [Fact]
    public async Task GetCalendarsAsync_ReturnsResultFromApi()
    {
        var (service, _) = MakeService("cal1");

        var result = await service.GetCalendarsAsync(2025);

        Assert.NotNull(result);
    }
}
