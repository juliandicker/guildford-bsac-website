using GuildfordBsac.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IO;
using System.Text;

namespace GuildfordBsac.Web.Tests;

public class FaqServiceTests
{
    private static FaqService MakeService(string contentRoot)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(contentRoot);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new FaqService(env.Object, cache, NullLogger<FaqService>.Instance);
    }

    [Fact]
    public async Task GetFaqsAsync_ValidFile_ReturnsFaqs()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(root, "App_Data"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "App_Data", "faqs.json"),
            """{"Faqs":[{"Question":"Q1","Answer":"<b>A1</b>"}]}""",
            Encoding.UTF8);

        var service = MakeService(root);
        var result = await service.GetFaqsAsync(FaqType.General);

        Assert.Single(result.Faqs);
        Assert.Equal("Q1", result.Faqs[0].Question);
    }

    [Fact]
    public async Task GetFaqsAsync_HtmlInAnswer_IsSanitized()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(root, "App_Data"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "App_Data", "faqs.json"),
            """{"Faqs":[{"Question":"Q","Answer":"<script>alert(1)</script>Safe text"}]}""",
            Encoding.UTF8);

        var service = MakeService(root);
        var result = await service.GetFaqsAsync(FaqType.General);

        Assert.DoesNotContain("<script>", result.Faqs[0].Answer);
        Assert.Contains("Safe text", result.Faqs[0].Answer);
    }

    [Fact]
    public async Task GetFaqsAsync_MissingFile_ReturnsEmptyViewModel()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(root, "App_Data"));
        // Do NOT create faqs.json

        var service = MakeService(root);
        var result = await service.GetFaqsAsync(FaqType.General);

        Assert.NotNull(result);
        Assert.Empty(result.Faqs);
    }

    [Fact]
    public async Task GetFaqsAsync_MalformedJson_ReturnsEmptyViewModel()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(root, "App_Data"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "App_Data", "faqs.json"),
            "{ this is not valid json }",
            Encoding.UTF8);

        var service = MakeService(root);
        var result = await service.GetFaqsAsync(FaqType.General);

        Assert.NotNull(result);
        Assert.Empty(result.Faqs);
    }

    [Fact]
    public async Task GetFaqsAsync_ContactType_LoadsContactFile()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(root, "App_Data"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "App_Data", "faqsContact.json"),
            """{"Faqs":[{"Question":"ContactQ","Answer":"ContactA"}]}""",
            Encoding.UTF8);

        var service = MakeService(root);
        var result = await service.GetFaqsAsync(FaqType.Contact);

        Assert.Single(result.Faqs);
        Assert.Equal("ContactQ", result.Faqs[0].Question);
    }
}
