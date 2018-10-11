namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Abstractions.Exceptions;
    using Microsoft.Extensions.Logging;

    internal sealed class DefaultClusterStateMonitor<TSchedulerKey, TJobKey> : IClusterStateMonitor<TSchedulerKey>
    {
        private readonly ISchedulerMetadataStore<TSchedulerKey> schedulerMetadataStore;
        private readonly IJobStore<TJobKey, TSchedulerKey> jobStore;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILogger<DefaultClusterStateMonitor<TSchedulerKey, TJobKey>> logger;

        public DefaultClusterStateMonitor(
            ISchedulerMetadataStore<TSchedulerKey> schedulerMetadataStore,
            IJobStore<TJobKey, TSchedulerKey> jobStore,
            IDateTimeProvider dateTimeProvider,
            ILogger<DefaultClusterStateMonitor<TSchedulerKey, TJobKey>> logger)
        {
            this.schedulerMetadataStore = schedulerMetadataStore;
            this.jobStore = jobStore;
            this.dateTimeProvider = dateTimeProvider;
            this.logger = logger;
        }

        public async Task MonitorClusterState(TSchedulerKey ownerSchedulerId, CancellationToken cancellationToken)
        {
            await this.MonitorStalledSchedulers(ownerSchedulerId, cancellationToken).ConfigureAwait(false);
            await this.MonitorTimeoutedJobs(cancellationToken).ConfigureAwait(false);
        }

        private Task MonitorStalledSchedulers(TSchedulerKey ownerSchedulerId, CancellationToken cancellationToken)
        {
            return this.DoChildAction(ct => this.MonitorStalledSchedulersInternal(ownerSchedulerId, ct), cancellationToken);
        }

        private Task MonitorTimeoutedJobs(CancellationToken cancellationToken)
        {
            return this.DoChildAction(ct => this.MonitorTimeoutedJobsInternal(ct), cancellationToken);
        }

        private async Task MonitorStalledSchedulersInternal(TSchedulerKey ownerSchedulerId, CancellationToken cancellationToken)
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
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await this.ProcessFailedScheduler(failedScheduler.id, failedScheduler.metadata).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Processing failed scheduler {0} failed: {1}", failedScheduler.id, ex.Message);
                }
            }
        }

        private async Task ProcessFailedScheduler(TSchedulerKey schedulerId, ISchedulerMetadata schedulerMetadata)
        {
            this.logger.LogWarning("Scheduler {0} seems to be stalled. Last heartbeat at {1}, Timeout {2}", schedulerId, schedulerMetadata.LastCheckin, schedulerMetadata.HeartbeatTimeout);

            try
            {
                await this.schedulerMetadataStore.RemoveScheduler(schedulerId).ConfigureAwait(false);
                this.logger.LogInformation("Scheduler {0} removed", schedulerId);
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogInformation(ex, "Scheduler {0} removed by someone else: {1}", schedulerId, ex.Message);
            }

            var executingJobs = await this.jobStore.GetExecutingJobs(schedulerId).ConfigureAwait(false);
            if (executingJobs.Count == 0)
            {
                this.logger.LogInformation("There's no jobs executing at stalled scheduler {0}", schedulerId);
                return;
            }

            this.logger.LogWarning("{0} jobs found to be executing at stalled scheduler {1}", executingJobs.Count, schedulerId);
            foreach (var stalledJob in executingJobs)
            {
                try
                {
                    await this.RecoverJob(stalledJob.id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Processing job {0} at stalled scheduler {1} failed: {2}", stalledJob.id, schedulerId, ex.Message);
                }
            }
        }

        private async Task RecoverJob(TJobKey jobKey)
        {
            try
            {
                await this.jobStore.RecoverJob(jobKey, JobExecutionResult.SchedulerStalled).ConfigureAwait(false);
                this.logger.LogInformation("Job {0} is recovered and rescheduled immediately. Result set to {1}", jobKey, JobExecutionResult.SchedulerStalled);
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogInformation(ex, "Job {0} recovered by someone else: {1}", jobKey, ex.Message);
            }
        }

        private async Task MonitorTimeoutedJobsInternal(CancellationToken cancellationToken)
        {
            var timeoutedJobs = await this.jobStore.GetTimeoutedJobs().ConfigureAwait(false);
            if (timeoutedJobs.Count == 0)
            {
                this.logger.LogTrace("No timeouted jobs detected");
                return;
            }

            this.logger.LogWarning("{0} timeouted jobs detected", timeoutedJobs.Count);

            foreach (var job in timeoutedJobs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await this.ProcessTimeoutedJob(job.id, job.metadata).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Processing timeouted job {0} failed: {1}", job.id, ex.Message);
                }
            }
        }

        private async Task ProcessTimeoutedJob(TJobKey jobId, IJobMetadata jobMetadata)
        {
            try
            {
                jobMetadata.SetNextExecutionTime();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to calculate execution time of job {0}: {1}. Job will be rescheduled immediately", jobId, ex.Message);
            }

            try
            {
                await this.jobStore.FinalizeJob(jobId, jobMetadata, JobExecutionResult.Timeouted).ConfigureAwait(false);
                this.logger.LogInformation("TImeouted job {0} rescheduled", jobId);
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogInformation(ex, "Job {0} was recovered by someone else: {1}", ex.Message);
            }
        }

        private async Task DoChildAction(Func<CancellationToken, Task> action, CancellationToken cancellationToken, [CallerMemberName] string callerMemberName = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
                this.logger.LogTrace("{0} completed in {1}", callerMemberName, sw.Elapsed);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                this.logger.LogWarning(ex, "{0} cancelled in {1}", callerMemberName, sw.Elapsed);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "{0} failed in {1}: {2}", callerMemberName, sw.Elapsed, ex.Message);
                throw;
            }
        }
    }
}
