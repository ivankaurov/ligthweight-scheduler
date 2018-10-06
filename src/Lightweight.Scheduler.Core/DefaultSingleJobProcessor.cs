﻿namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Abstractions.Exceptions;
    using Lightweight.Scheduler.Core.Internal;
    using Microsoft.Extensions.Logging;

    internal sealed class DefaultSingleJobProcessor<TSchedulerKey, TJobKey> : ISingleJobProcessor<TSchedulerKey, TJobKey>
    {
        private readonly IJobStore<TJobKey, TSchedulerKey> jobStore;
        private readonly ISyncHelper syncHelper;
        private readonly IJobFactory jobFactory;
        private readonly ILogger<DefaultSingleJobProcessor<TSchedulerKey, TJobKey>> logger;

        public DefaultSingleJobProcessor(
            IJobStore<TJobKey, TSchedulerKey> jobStore,
            ISyncHelper syncHelper,
            IJobFactory jobFactory,
            ILogger<DefaultSingleJobProcessor<TSchedulerKey, TJobKey>> logger)
        {
            this.jobStore = jobStore;
            this.syncHelper = syncHelper;
            this.jobFactory = jobFactory;
            this.logger = logger;
        }

        public async Task<bool> ProcessSingleJob(
            IIdentifier<TJobKey> jobId,
            IJobMetadata jobMetadata,
            IIdentifier<TSchedulerKey> schedluerId,
            CancellationToken cancellationToken)
        {
            if (!await this.TryCaptureExecutionThread(cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            try
            {
                if (!await this.TrySetJobOwner(jobId, schedluerId).ConfigureAwait(false))
                {
                    return false;
                }

                try
                {
                    this.logger.LogInformation("Starting job {0} execution at scheduler {1}", jobId, schedluerId);
                    await this.ExecuteJob(jobMetadata, cancellationToken).ConfigureAwait(false);
                    this.logger.LogInformation("Job {0} execution completed", jobId);
                    return true;
                }
                finally
                {
                    await this.FinishJobExecution(jobId, jobMetadata).ConfigureAwait(false);
                }
            }
            finally
            {
                this.syncHelper.Release();
            }
        }

        private async Task<bool> TryCaptureExecutionThread(CancellationToken cancellationToken)
        {
            var result = await this.syncHelper.WaitOne(cancellationToken).ConfigureAwait(false);
            if (!result)
            {
                this.logger.LogTrace("All threads are busy - exiting");
            }

            return result;
        }

        private async Task<bool> TrySetJobOwner(IIdentifier<TJobKey> jobId, IIdentifier<TSchedulerKey> schedulerId)
        {
            try
            {
                await this.jobStore.SetJobOwner(jobId, schedulerId).ConfigureAwait(false);
                return true;
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogTrace(ex, "Job {0} is already executing on another scheduler", jobId);
                return false;
            }
        }

        private async Task ExecuteJob(IJobMetadata jobMetadata, CancellationToken cancellationToken)
        {
            if (jobMetadata.Timeout > TimeSpan.Zero && jobMetadata.Timeout != Timeout.InfiniteTimeSpan)
            {
                using (var timeoutCts = new CancellationTokenSource(jobMetadata.Timeout.Value))
                {
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                    {
                        await this.ExecuteJobInternal(jobMetadata, linkedCts.Token).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await this.ExecuteJobInternal(jobMetadata, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ExecuteJobInternal(IJobMetadata jobMetadata, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (this.jobFactory.BeginScope())
            {
                var job = this.jobFactory.CreateJobInstance(jobMetadata);
                this.logger.LogTrace("Job created");

                var sw = Stopwatch.StartNew();
                await job.Invoke(jobMetadata.Context, cancellationToken).ConfigureAwait(false);
                sw.Stop();
                this.logger.LogTrace("Job executed successfully in {0}", sw.Elapsed);
            }

            jobMetadata.SetNextExecutionTime();
            this.logger.LogTrace("Next execution time updated");
        }

        private async Task FinishJobExecution(IIdentifier<TJobKey> jobId, IJobMetadata jobMetadata)
        {
            try
            {
                await this.jobStore.UpdateJob(jobId, jobMetadata, null).ConfigureAwait(false);
                this.logger.LogTrace("Job {0} state cleared", jobId);
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogError(ex, "Clearing job {0} state unexpectedly failed with concurrency exception: {1}", jobId, ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Clearing job {0} state failed unexpectedly: {1}", jobId, ex.Message);
            }
        }
    }
}
