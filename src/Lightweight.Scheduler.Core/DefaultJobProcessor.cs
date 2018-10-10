namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Microsoft.Extensions.Logging;

    internal sealed class DefaultJobProcessor<TSchedulerKey, TJobKey> : IJobProcessor<TSchedulerKey>
    {
        private readonly ISingleJobProcessor<TSchedulerKey, TJobKey> singleJobProcessor;
        private readonly IJobStore<TJobKey, TSchedulerKey> jobStore;
        private readonly ILogger<DefaultJobProcessor<TSchedulerKey, TJobKey>> logger;

        public DefaultJobProcessor(
            ISingleJobProcessor<TSchedulerKey, TJobKey> singleJobProcessor,
            IJobStore<TJobKey, TSchedulerKey> jobStore,
            ILogger<DefaultJobProcessor<TSchedulerKey, TJobKey>> logger)
        {
            this.singleJobProcessor = singleJobProcessor;
            this.jobStore = jobStore;
            this.logger = logger;
        }

        public async Task ProcessJobs(IIdentifier<TSchedulerKey> schedulerId, CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Geting jobs for scheduler {0}", schedulerId);

            var jobs = await this.jobStore.GetJobsForExecution().ConfigureAwait(false);
            this.logger.LogInformation("{0} jobs got", jobs.Count);

            if (jobs.Count > 0)
            {
                Parallel.ForEach(
                    jobs,
                    new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        TaskScheduler = TaskScheduler.Default,
                    },
                    j => this.ProcessSingleJob(j.id, j.metadata, schedulerId, cancellationToken));
            }
        }

        private async void ProcessSingleJob(
            IIdentifier<TJobKey> jobId,
            IJobMetadata jobMetadata,
            IIdentifier<TSchedulerKey> schedluerId,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                this.logger.LogTrace("Trying to execute job {0} at scheduler {1}", jobId, schedluerId);
                await Task.Yield();
                var res = await this.singleJobProcessor.ProcessSingleJob(jobId, jobMetadata, schedluerId, cancellationToken).ConfigureAwait(false);

                sw.Stop();
                this.logger.LogInformation("Job {0} executed at scheduler {1} in {2}. Result: {3}", jobId, schedluerId, sw.Elapsed, res);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                this.logger.LogWarning(ex, "Job {0} has been canceled due scheduler {1} cancellation after {2}", jobId, schedluerId, sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                this.logger.LogError(ex, "Job {0} failed at scheduler {1} in {2}: {3}", jobId, schedluerId, sw.Elapsed, ex.Message);
            }
        }
    }
}
