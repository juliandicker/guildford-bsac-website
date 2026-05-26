namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Configuration;
    using GuildfordBsac.Web.Models;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class FacebookService : IFacebookService, IAsyncDisposable
    {
        private readonly FacebookSettings _settings;
        private readonly ILogger<FacebookService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CachedFetcher<List<FacebookPostModel>> _fetcher;
        private int _consecutiveFailures;

        public FacebookService(IMemoryCache cache, IOptions<FacebookSettings> settings, ILogger<FacebookService> logger, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _fetcher = new CachedFetcher<List<FacebookPostModel>>(cache);
        }

        public async Task<List<FacebookPostModel>> GetRecentPostsAsync(int limit = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.PageAccessToken))
                return new List<FacebookPostModel>();

            return await _fetcher.GetOrFetchAsync(
                "FacebookPosts",
                ct => FetchFromApiAsync(limit, ct),
                cancellationToken) ?? new List<FacebookPostModel>();
        }

        private async Task<(List<FacebookPostModel>? Value, TimeSpan CacheDuration)> FetchFromApiAsync(int limit, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PageId}/posts" +
                          $"?fields=message,story,created_time,full_picture,permalink_url" +
                          $"&limit={limit}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.PageAccessToken);
                var client = _httpClientFactory.CreateClient("facebook");
                var httpResponse = await client.SendAsync(request, cancellationToken);
                var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Facebook API {StatusCode}: {Body}", (int)httpResponse.StatusCode, json);
                    return (new List<FacebookPostModel>(), GetErrorCacheDuration());
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
                    return (new List<FacebookPostModel>(), GetErrorCacheDuration());
                }

                Interlocked.Exchange(ref _consecutiveFailures, 0);
                return (result, _settings.SuccessCacheDuration);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Facebook posts for page {PageId}", _settings.PageId);
                return (new List<FacebookPostModel>(), GetErrorCacheDuration());
            }
        }

        // Exponential backoff: 90s → 15m → 2h max, to avoid hammering the API during outages
        private TimeSpan GetErrorCacheDuration()
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            if (failures <= 1) return _settings.ErrorCacheDurationInitial;
            if (failures <= 3) return TimeSpan.FromMinutes(15);
            return _settings.ErrorCacheDurationMax;
        }

        public ValueTask DisposeAsync() => _fetcher.DisposeAsync();
    }
}
