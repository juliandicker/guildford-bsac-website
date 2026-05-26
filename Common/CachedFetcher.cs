namespace GuildfordBsac.Web.Common
{
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    // Thread-safe cache population using double-checked locking with a bounded lock timeout.
    // The fetch delegate controls both the result value and its cache TTL, so callers can
    // return different durations for success vs. error paths. Return TimeSpan.Zero to skip caching.
    internal sealed class CachedFetcher<TValue> : IAsyncDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);

        internal CachedFetcher(IMemoryCache cache) => _cache = cache;

        internal async Task<TValue?> GetOrFetchAsync(
            string key,
            Func<CancellationToken, Task<(TValue? Value, TimeSpan CacheDuration)>> fetch,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out TValue? hit))
                return hit;

            using var lockCts = new CancellationTokenSource(LockTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lockCts.Token);

            await _lock.WaitAsync(linked.Token);
            try
            {
                if (_cache.TryGetValue(key, out hit))
                    return hit;

                var (value, duration) = await fetch(cancellationToken);
                if (duration > TimeSpan.Zero)
                    _cache.Set(key, value, duration);
                return value;
            }
            finally
            {
                _lock.Release();
            }
        }

        public ValueTask DisposeAsync()
        {
            _lock.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
