using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace GuildfordBsac.Web.Tests;

public class GuildfordBsacWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Navigate from bin/Release|Debug/net8.0/ up to the repo root where App_Data lives
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        builder.UseContentRoot(webRoot);
        builder.UseEnvironment("Development");
    }
}

public class IntegrationTests : IClassFixture<GuildfordBsacWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IntegrationTests(GuildfordBsacWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/About")]
    [InlineData("/Home/PrivacyPolicy")]
    [InlineData("/Home/CodeOfConduct")]
    [InlineData("/Home/ContactUs")]
    [InlineData("/Home/Faqs")]
    [InlineData("/Training")]
    public async Task Get_ReturnsOk(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
