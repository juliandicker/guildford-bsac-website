using GuildfordBsac.Web.Common;
using Google.Apis.Calendar.v3.Data;

namespace GuildfordBsac.Web.Tests;

public class CalendarAdapterTests
{
    private static CalendarListEntry Cal(string name = "Test") =>
        new() { Summary = name, BackgroundColor = "#000000" };

    private static Events Evts(params Event[] items) =>
        new() { Items = items.ToList() };

    private static Event AllDay(string start, string end, string summary = "Event") =>
        new()
        {
            Summary = summary,
            Start = new EventDateTime { Date = start },
            End = new EventDateTime { Date = end }
        };

    [Fact]
    public void SingleDayEvent_IsNotSplit()
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(AllDay("2024-03-15", "2024-03-16")));

        Assert.Single(adapter.GetCalendars()[0].Events);
    }

    [Fact]
    public void MultiDayEventWithinSameMonth_IsNotSplit()
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(AllDay("2024-03-10", "2024-03-20")));

        Assert.Single(adapter.GetCalendars()[0].Events);
    }

    [Fact]
    public void AllDayEventEndingOnFirstOfNextMonth_IsNotSplit()
    {
        // iCal all-day "March 31" events have end = April 1 (exclusive); should stay in March
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(AllDay("2024-03-31", "2024-04-01")));

        Assert.Single(adapter.GetCalendars()[0].Events);
    }

    [Fact]
    public void EventSpanningTwoMonths_IsSplitAtMonthBoundary()
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(AllDay("2024-03-15", "2024-04-05")));

        var events = adapter.GetCalendars()[0].Events;
        Assert.Equal(2, events.Count);
        Assert.Equal(new DateTime(2024, 3, 15), events[0].StartDate);
        Assert.Equal(new DateTime(2024, 4, 1), events[0].EndDate);
        Assert.Equal(new DateTime(2024, 4, 1), events[1].StartDate);
        Assert.Equal(new DateTime(2024, 4, 5), events[1].EndDate);
    }

    [Fact]
    public void SplitEvent_SummaryPreservedOnBothParts()
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(AllDay("2024-03-15", "2024-04-05", "Dive Trip")));

        Assert.All(adapter.GetCalendars()[0].Events, e => Assert.Equal("Dive Trip", e.Summary));
    }

    [Fact]
    public void SplitDisabled_MultiMonthEventIsNotSplit()
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(AllDay("2024-03-15", "2024-04-05")), splitEventsOverMonths: false);

        Assert.Single(adapter.GetCalendars()[0].Events);
    }

    [Fact]
    public void TimedEventShorterThanOneDay_DurationIsOne()
    {
        var adapter = new CalendarAdapter();
        var evt = new Event
        {
            Summary = "Meeting",
            Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(2024, 3, 15, 9, 0, 0, TimeSpan.Zero) },
            End = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero) }
        };
        adapter.AddCalendar(Cal(), Evts(evt));

        Assert.Equal(1.0, adapter.GetCalendars()[0].Events[0].Duration);
    }

    [Fact]
    public void MultiDayEvent_DurationMatchesDayCount()
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(AllDay("2024-03-10", "2024-03-15")));

        Assert.Equal(5.0, adapter.GetCalendars()[0].Events[0].Duration);
    }

    [Fact]
    public void NullEventItems_ProducesEmptyList()
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), new Events { Items = null });

        Assert.Empty(adapter.GetCalendars()[0].Events);
    }
}
