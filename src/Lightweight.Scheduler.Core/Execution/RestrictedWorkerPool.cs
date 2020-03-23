namespace Lightweight.Scheduler.Core.Execution
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Lightweight.Scheduler.Abstractions.Internal;
    using Lightweight.Scheduler.Abstractions.Internal.Exceptions;
    using Lightweight.Scheduler.Core.Configuration;

    using Microsoft.Extensions.Options;

    internal sealed class RestrictedWorkerPool : IWorkerPool, IDisposable
    {
        private readonly SemaphoreSlim syncRoot;

        private readonly WorkerPoolOptions options;

        private bool objectDisposed;

        public RestrictedWorkerPool(IOptions<WorkerPoolOptions> options)
        {
            this.options = options.Value;
            this.syncRoot = new SemaphoreSlim(this.options.Capacity, this.options.Capacity);
        }

        public int AvailableWorkers
        {
            get
            {
                this.CheckIfDisposed();
                return this.syncRoot.CurrentCount;
            }
        }

        public async Task InvokeOnPool(Func<Task> action, CancellationToken cancellationToken)
        {
            this.CheckIfDisposed();

            if (!await this.syncRoot.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
            {
                throw new TaskRejectedException();
            }

            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                this.syncRoot.Release();
            }
        }

        public void Dispose()
        {
            if (!this.objectDisposed)
            {
                this.syncRoot.Dispose();
                this.objectDisposed = true;
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