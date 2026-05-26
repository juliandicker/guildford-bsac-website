namespace GuildfordBsac.Web.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    // Singleton that serialises concurrent Rotativa PNG render subprocess invocations.
    // wkhtmltopdf is heavyweight; allowing stampede on cache miss exhausts process handles.
    public sealed class PngRenderLock : IAsyncDisposable
    {
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(120);
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(WaitTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await _semaphore.WaitAsync(linked.Token);
        }

        public void Release() => _semaphore.Release();

        public ValueTask DisposeAsync()
        {
            _semaphore.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
