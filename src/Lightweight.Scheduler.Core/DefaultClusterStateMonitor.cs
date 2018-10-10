namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Microsoft.Extensions.Logging;

    internal sealed class DefaultClusterStateMonitor<TSchedulerKey> : IClusterStateMonitor<TSchedulerKey>
    {
        private readonly ISchedulerMetadataStore<TSchedulerKey> schedulerMetadataStore;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILogger<DefaultClusterStateMonitor<TSchedulerKey>> logger;

        public DefaultClusterStateMonitor(
            ISchedulerMetadataStore<TSchedulerKey> schedulerMetadataStore,
            IDateTimeProvider dateTimeProvider,
            ILogger<DefaultClusterStateMonitor<TSchedulerKey>> logger)
        {
            this.schedulerMetadataStore = schedulerMetadataStore;
            this.dateTimeProvider = dateTimeProvider;
            this.logger = logger;
        }

        public async Task MonitorClusterState(TSchedulerKey ownerSchedulerId, CancellationToken cancellationToken)
        {
            var schedulers = await this.schedulerMetadataStore.GetSchedulers().ConfigureAwait(false);
            this.logger.LogTrace("{0} scheduler(s) found", schedulers.Count);
            if (!schedulers.Any(s => s.id.Equals(ownerSchedulerId)))
            {
                this.logger.LogWarning("Own scheduler not found");
            }

            var now = this.dateTimeProvider.Now();
            var failedSchedulers = schedulers.Where(s => !s.id.Equals(ownerSchedulerId) && (now - s.metadata.LastCheckin) > s.metadata.HeartbeatTimeout);

            foreach (var failedScheduler in failedSchedulers)
            {
                try
                {
                    await this.ProcessFailedScheduler(failedScheduler.id, failedScheduler.metadata, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    this.logger.LogWarning(ex, "Processing failed scheduler {0} interrupted: {1}", failedScheduler.id, ex.Message);
                    return;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Processing failed scheduler {0} failed: {1}", failedScheduler.id, ex.Message);
                }
            }
        }

        private Task ProcessFailedScheduler(TSchedulerKey schedulerId, ISchedulerMetadata schedulerMetadata, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
