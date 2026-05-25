using GuildfordBsac.Web.Models;
using System;

namespace GuildfordBsac.Web.Tests;

public class FacebookModelTests
{
    [Fact]
    public void FacebookPostModel_Deserializes_PlusZeroZeroZeroZeroOffset()
    {
        var json = """
            {
                "id": "123456",
                "message": "Test post",
                "created_time": "2026-05-01T10:30:00+0000",
                "permalink_url": "https://www.facebook.com/permalink/123456"
            }
            """;

        var post = JsonSerializer.Deserialize<FacebookPostModel>(json);

        Assert.NotNull(post);
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero), post.CreatedTime);
    }

    [Fact]
    public void FacebookPostModel_Deserializes_StandardIsoOffset()
    {
        var json = """
            {
                "id": "123456",
                "created_time": "2026-05-01T10:30:00+00:00",
                "permalink_url": "https://www.facebook.com/permalink/123456"
            }
            """;

        var post = JsonSerializer.Deserialize<FacebookPostModel>(json);

        Assert.NotNull(post);
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero), post.CreatedTime);
    }
}
