namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Microsoft.Extensions.Logging;

    internal sealed class DefaultScheduler<TStorageKey> : IScheduler, IDisposable
    {
        private readonly TStorageKey schedulerId;
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
            this.schedulerId = schedulerId;
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

        public async Task Start()
        {
            this.CheckIfDisposed();
            if (this.state != State.Created)
            {
                throw new InvalidOperationException($"Invalid state: Required {State.Created}, Current {this.state}");
            }

            await this.InitScheduler().ConfigureAwait(false);

            this.Main();
        }

        public async Task Stop()
        {
            this.CheckIfDisposed();
            if (this.state != State.Running)
            {
                throw new InvalidOperationException($"Invalid state: Required {State.Running}, Current {this.state}");
            }

            this.cancellationTokenSource.Cancel();
            try
            {
                await this.schedulerMetadataStore.RemoveScheduler(this.schedulerId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to remove scheduler from store: {0}", ex.Message);
            }

            this.logger.LogInformation("Scheduler {0} stopped", this.schedulerId);
            this.state = State.Stopped;
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

        private async Task InitScheduler()
        {
            try
            {
                await this.schedulerMetadataStore.AddScheduler(this.schedulerId, this.metadata).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Can't add scheduler {0} to store: {1}. Schduler won't start", this.schedulerId, ex.Message);
                throw;
            }
        }

        private async void Main()
        {
            await Task.Yield();
            this.state = State.Running;

            this.logger.LogInformation("Scheduler {0} is running", this.schedulerId);

            var stopwatch = new Stopwatch();
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    stopwatch.Restart();

                    await this.DoHeartbeat().ConfigureAwait(false);
                    await this.DoClusterMonitoring().ConfigureAwait(false);
                    await this.ProcessJobs().ConfigureAwait(false);

                    stopwatch.Stop();
                    await this.WaitForNextIteration(stopwatch.Elapsed).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (this.cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Main cycle failed: {0}", ex.Message);
                }
            }

            this.logger.LogInformation("Scheduler {0}: Main thread exiting", this.schedulerId);
        }

        private Task DoHeartbeat()
        {
            return this.DoChildAction(() => this.schedulerMetadataStore.Heartbeat(this.schedulerId));
        }

        private Task DoClusterMonitoring()
        {
            return this.DoChildAction(() => this.schedulerStateMonitor.MonitorClusterState(this.schedulerId, this.cancellationTokenSource.Token));
        }

        private Task ProcessJobs()
        {
            return this.DoChildAction(() => this.jobProcessor.ProcessJobs(this.schedulerId, this.cancellationTokenSource.Token));
        }

        private async Task DoChildAction(Func<Task> action, [CallerMemberName] string callerMemberName = null)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (this.cancellationTokenSource.IsCancellationRequested)
            {
                this.logger.LogWarning(ex, "{0} is canceled - schduler stopped", callerMemberName);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "{0} failed: {1}", callerMemberName, ex);
            }
        }

        private async Task WaitForNextIteration(TimeSpan iteration)
        {
            if (iteration < this.metadata.HeartbeatInterval)
            {
                await Task.Delay(this.metadata.HeartbeatInterval - iteration, this.cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }
    }
}
