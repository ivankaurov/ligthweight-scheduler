namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Microsoft.Extensions.Logging;

    internal sealed class DefaultScheduler<TStorageKey> : IScheduler, ISchedulerMetadata, IIdentifier<TStorageKey>, IDisposable
    {
        private readonly ISchedulerMetadata metadata;
        private readonly ISchedulerMetadataStore<TStorageKey> schedulerMetadataStore;
        private readonly ILogger<DefaultScheduler<TStorageKey>> logger;
        private readonly TStorageKey schedulerId;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private bool objectDisposed;

        private State state = State.Created;

        public DefaultScheduler(
            TStorageKey schedulerId,
            ISchedulerMetadata schedulerMetadata,
            ISchedulerMetadataStore<TStorageKey> schedulerMetadataStore,
            ILogger<DefaultScheduler<TStorageKey>> logger)
        {
            this.schedulerId = schedulerId;
            this.metadata = schedulerMetadata;
            this.schedulerMetadataStore = schedulerMetadataStore;
            this.logger = logger;
        }

        private enum State
        {
            Created,
            Running,
            Stopped,
        }

        public TimeSpan HeartbeatInterval => this.metadata.HeartbeatInterval;

        public TimeSpan HeartbeatTimeout => this.metadata.HeartbeatTimeout;

        public TStorageKey Id => this.schedulerId;

        public async Task Start()
        {
            this.CheckIfDisposed();
            if (this.state != State.Created)
            {
                throw new InvalidOperationException($"Invalid state: Required {State.Created}, Current {this.state}");
            }

            await this.InitScheduler().ConfigureAwait(false);

            ThreadPool.QueueUserWorkItem(_ => this.Main().GetAwaiter().GetResult());
        }

        public Task Stop()
        {
            this.CheckIfDisposed();
            if (this.state != State.Running)
            {
                throw new InvalidOperationException($"Invalid state: Required {State.Running}, Current {this.state}");
            }

            this.cancellationTokenSource.Cancel();
            this.state = State.Stopped;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!this.objectDisposed)
            {
                this.cancellationTokenSource.Dispose();
                this.objectDisposed = true;
            }
        }

        private void CheckIfDisposed()
        {
            if (this.objectDisposed)
            {
                throw new ObjectDisposedException($"{this.GetType().Name} {{{this.schedulerId}}}");
            }
        }

        private Task InitScheduler()
        {
            return Task.CompletedTask;
        }

        private async Task Main()
        {
            var stopwatch = new Stopwatch();
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                stopwatch.Restart();

                await this.DoHeartbeat().ConfigureAwait(false);

                stopwatch.Stop();
                await this.WaitForNextIteration(stopwatch.Elapsed).ConfigureAwait(false);
            }
        }

        private async Task DoHeartbeat()
        {
            try
            {
                await this.schedulerMetadataStore.Heartbeat(this).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Heartbeat failed: {0}", ex.Message);
            }
        }

        private async Task WaitForNextIteration(TimeSpan iteration)
        {
            if (iteration < this.HeartbeatInterval)
            {
                await Task.Delay(this.HeartbeatInterval - iteration, this.cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }
    }
}
