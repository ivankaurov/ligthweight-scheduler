namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Microsoft.Extensions.Logging;

    internal sealed class DefaultScheduler<TStorageKey> : IScheduler, ISchedulerMetadata, IIdentifier<TStorageKey>, IDisposable
    {
        private readonly ISchedulerMetadata metadata;
        private readonly ISchedulerMetadataStore<TStorageKey> schedulerMetadataStore;
        private readonly ISchedulerStateMonitor<TStorageKey> schedulerStateMonitor;
        private readonly IJobProcessor<TStorageKey> jobProcessor;
        private readonly ILogger<DefaultScheduler<TStorageKey>> logger;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private bool objectDisposed;

        private State state = State.Created;

        public DefaultScheduler(
            TStorageKey schedulerId,
            ISchedulerMetadata schedulerMetadata,
            ISchedulerMetadataStore<TStorageKey> schedulerMetadataStore,
            ISchedulerStateMonitor<TStorageKey> schedulerStateMonitor,
            IJobProcessor<TStorageKey> jobProcessor,
            ILogger<DefaultScheduler<TStorageKey>> logger)
        {
            this.Id = schedulerId;
            this.metadata = schedulerMetadata;
            this.schedulerMetadataStore = schedulerMetadataStore;
            this.schedulerStateMonitor = schedulerStateMonitor;
            this.jobProcessor = jobProcessor;
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

        public TStorageKey Id { get; }

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
                throw new ObjectDisposedException($"{this.GetType().Name} {{{this.Id}}}");
            }
        }

        private Task InitScheduler()
        {
            return Task.CompletedTask;
        }

        private async Task Main()
        {
            this.state = State.Running;
            var stopwatch = new Stopwatch();
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                stopwatch.Restart();

                await this.DoHeartbeat().ConfigureAwait(false);
                await this.DoClusterMonitoring().ConfigureAwait(false);
                await this.ProcessJobs().ConfigureAwait(false);

                stopwatch.Stop();
                await this.WaitForNextIteration(stopwatch.Elapsed).ConfigureAwait(false);
            }
        }

        private Task DoHeartbeat()
        {
            return this.DoChildAction(() => this.schedulerMetadataStore.Heartbeat(this));
        }

        private Task DoClusterMonitoring()
        {
            return this.DoChildAction(() => this.schedulerStateMonitor.MonitorClusterState(this, this.cancellationTokenSource.Token));
        }

        private Task ProcessJobs()
        {
            return this.DoChildAction(() => this.jobProcessor.ProcessJobs(this, this.cancellationTokenSource.Token));
        }

        private async Task DoChildAction(Func<Task> action, [CallerMemberName] string callerMemberName = null)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                this.logger.LogWarning(ex, "{0} is cancelled", callerMemberName);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "{0} failed: {1}", callerMemberName, ex);
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
