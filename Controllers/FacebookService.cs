namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Models;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class FacebookService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<FacebookService> _logger;
        private readonly string _accessToken;
        private static readonly HttpClient _http = new HttpClient();

        public FacebookService(IMemoryCache cache, IConfiguration config, ILogger<FacebookService> logger)
        {
            _cache = cache;
            _logger = logger;
            _accessToken = config["Facebook:PageAccessToken"] ?? "";
        }

        public async Task<List<FacebookPostModel>> GetRecentPostsAsync(string pageId, int limit = 5)
        {
            const string cacheKey = "FacebookPosts";

            if (_cache.TryGetValue(cacheKey, out List<FacebookPostModel>? cached) && cached != null)
                return cached;

            if (string.IsNullOrWhiteSpace(_accessToken))
                return new List<FacebookPostModel>();

            try
            {
                var url = $"https://graph.facebook.com/v25.0/{pageId}/posts" +
                          $"?fields=message,story,created_time,full_picture,permalink_url" +
                          $"&limit={limit}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                var httpResponse = await _http.SendAsync(request);
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Facebook API {StatusCode}: {Body}", (int)httpResponse.StatusCode, json);
                    return new List<FacebookPostModel>();
                }
                var response = JsonSerializer.Deserialize<FacebookPageResponseModel>(json);
                var posts = response?.Data ?? new List<FacebookPostModel>();

                _cache.Set(cacheKey, posts, TimeSpan.FromMinutes(30));

                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Facebook posts for page {PageId}", pageId);
                return new List<FacebookPostModel>();
            }
        }
    }
}
