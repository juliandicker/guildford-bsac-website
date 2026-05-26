namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IFacebookService
    {
        Task<List<FacebookPostModel>> GetRecentPostsAsync(int limit = 5, CancellationToken cancellationToken = default);
    }

    public class FacebookService : IFacebookService
    {
        private const string PageId = "1027783460591236";

        private readonly IMemoryCache _cache;
        private readonly ILogger<FacebookService> _logger;
        private readonly string _accessToken;
        private readonly IHttpClientFactory _httpClientFactory;

        public FacebookService(IMemoryCache cache, IConfiguration config, ILogger<FacebookService> logger, IHttpClientFactory httpClientFactory)
        {
            _cache = cache;
            _logger = logger;
            _accessToken = config["Facebook:PageAccessToken"] ?? "";
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<FacebookPostModel>> GetRecentPostsAsync(int limit = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_accessToken))
                return new List<FacebookPostModel>();

            try
            {
                return await _cache.GetOrCreateAsync("FacebookPosts", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

                    var url = $"https://graph.facebook.com/v25.0/{PageId}/posts" +
                              $"?fields=message,story,created_time,full_picture,permalink_url" +
                              $"&limit={limit}";

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    var client = _httpClientFactory.CreateClient("facebook");
                    var httpResponse = await client.SendAsync(request, cancellationToken);
                    var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("Facebook API {StatusCode}: {Body}", (int)httpResponse.StatusCode, json);
                        return new List<FacebookPostModel>();
                    }

                    var response = JsonSerializer.Deserialize<FacebookPageResponseModel>(json);
                    return response?.Data ?? new List<FacebookPostModel>();
                }) ?? new List<FacebookPostModel>();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Facebook posts for page {PageId}", PageId);
                return new List<FacebookPostModel>();
            }
        }
    }
}
