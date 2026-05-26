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
        private const string ApiVersion = "v25.0";
        private static readonly TimeSpan SuccessCacheDuration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan ErrorCacheDuration = TimeSpan.FromSeconds(90);

        private readonly IMemoryCache _cache;
        private readonly ILogger<FacebookService> _logger;
        private readonly string _accessToken;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SemaphoreSlim _fetchLock = new SemaphoreSlim(1, 1);

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

            const string cacheKey = "FacebookPosts";

            if (_cache.TryGetValue(cacheKey, out List<FacebookPostModel>? cached))
                return cached!;

            await _fetchLock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(cacheKey, out cached))
                    return cached!;

                return await FetchAndCacheAsync(cacheKey, limit, cancellationToken);
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        private async Task<List<FacebookPostModel>> FetchAndCacheAsync(string cacheKey, int limit, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"https://graph.facebook.com/{ApiVersion}/{PageId}/posts" +
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
                    _cache.Set(cacheKey, new List<FacebookPostModel>(), ErrorCacheDuration);
                    return new List<FacebookPostModel>();
                }

                List<FacebookPostModel> result;
                try
                {
                    var response = JsonSerializer.Deserialize<FacebookPageResponseModel>(json);
                    result = response?.Data ?? new List<FacebookPostModel>();
                }
                catch (JsonException jex)
                {
                    _logger.LogError(jex, "Unexpected Facebook API response body (JSON parse failure)");
                    _cache.Set(cacheKey, new List<FacebookPostModel>(), ErrorCacheDuration);
                    return new List<FacebookPostModel>();
                }

                _cache.Set(cacheKey, result, SuccessCacheDuration);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Facebook posts for page {PageId}", PageId);
                _cache.Set(cacheKey, new List<FacebookPostModel>(), ErrorCacheDuration);
                return new List<FacebookPostModel>();
            }
        }
    }
}
