namespace Lightweight.Scheduler.Core.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class SemaphoreSyncHelper : ISyncHelper, IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim;

        private bool objectDisposed;

        public SemaphoreSyncHelper(int maxThreads)
        {
            if (maxThreads < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxThreads), "value should be positive");
            }

            this.semaphoreSlim = new SemaphoreSlim(maxThreads, maxThreads);
        }

        public void Release()
        {
            this.CheckIfDisposed();
            this.semaphoreSlim.Release();
        }

        public Task<bool> WaitOne(CancellationToken cancellationToken)
        {
            this.CheckIfDisposed();
            return this.semaphoreSlim.WaitAsync(0, cancellationToken);
        }

        public void Dispose()
        {
            if (!this.objectDisposed)
            {
                this.objectDisposed = true;
                this.semaphoreSlim.Dispose();
            }
        }

        private void CheckIfDisposed()
        {
            if (this.objectDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }
}
