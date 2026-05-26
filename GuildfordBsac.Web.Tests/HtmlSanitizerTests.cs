using GuildfordBsac.Web.Common;
using Google.Apis.Calendar.v3.Data;

namespace GuildfordBsac.Web.Tests;

// Tests that SharedHtmlSanitizer (used by CalendarAdapter and FaqService) actually strips
// dangerous payloads. Verified indirectly via CalendarAdapter to avoid exposing internals.
public class HtmlSanitizerTests
{
    private static CalendarListEntry Cal() =>
        new() { Summary = "Test", BackgroundColor = "#000000" };

    private static Events Evts(string description) =>
        new()
        {
            Items = new List<Event>
            {
                new()
                {
                    Summary = "Test Event",
                    Description = description,
                    Start = new EventDateTime { Date = "2024-03-15" },
                    End = new EventDateTime { Date = "2024-03-16" }
                }
            }
        };

    private static string? GetDescription(string raw)
    {
        var adapter = new CalendarAdapter();
        adapter.AddCalendar(Cal(), Evts(raw));
        return adapter.GetCalendars()[0].Events[0].Description;
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<script src='http://evil.com/xss.js'></script>")]
    [InlineData("<SCRIPT>alert(document.cookie)</SCRIPT>")]
    public void ScriptTag_IsStripped(string payload)
    {
        var result = GetDescription(payload);
        Assert.DoesNotContain("<script", result ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<body onload=alert(1)>")]
    [InlineData("<a href='#' onclick='evil()'>click</a>")]
    public void EventHandlerAttributes_AreStripped(string payload)
    {
        var result = GetDescription(payload);
        Assert.DoesNotContain("onerror", result ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", result ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", result ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<iframe src='http://evil.com'></iframe>")]
    [InlineData("<object data='http://evil.com'></object>")]
    [InlineData("<embed src='http://evil.com'>")]
    public void DangerousTags_AreStripped(string payload)
    {
        var result = GetDescription(payload);
        Assert.DoesNotContain("<iframe", result ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<object", result ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<embed", result ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<a href='javascript:alert(1)'>click</a>")]
    [InlineData("<a href='JAVASCRIPT:void(0)'>click</a>")]
    public void JavascriptScheme_IsStripped(string payload)
    {
        var result = GetDescription(payload);
        Assert.DoesNotContain("javascript:", result ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllowedTags_ArePreserved()
    {
        const string safe = "<b>bold</b> <i>italic</i> <strong>strong</strong> <em>em</em> <br> " +
                            "<p>para</p> <ul><li>item</li></ul> <ol><li>item</li></ol> " +
                            "<a href='https://example.com'>link</a>";
        var result = GetDescription(safe);
        Assert.Contains("<b>", result ?? "");
        Assert.Contains("<a href", result ?? "");
    }

    [Fact]
    public void CssInjectionViaStyleAttribute_IsStripped()
    {
        const string payload = "<p style='background:url(javascript:alert(1))'>text</p>";
        var result = GetDescription(payload);
        Assert.DoesNotContain("style=", result ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
