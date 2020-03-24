namespace Lightweight.Scheduler.Core.Execution
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Lightweight.Scheduler.Abstractions.Internal;
    using Lightweight.Scheduler.Abstractions.Internal.Exceptions;
    using Lightweight.Scheduler.Utils;

    using Microsoft.Extensions.Logging;

    internal sealed class ExecutionCore<TJobId, TNodeId, TVersion> : ITickHandler
    {
        private readonly IJobStore<TJobId, TNodeId, TVersion> jobStore;

        private readonly IWorkerPool workerPool;

        private readonly IJobExecutor<TJobId, TVersion> jobExecutor;

        private readonly ILogger<ExecutionCore<TJobId, TNodeId, TVersion>> logger;

        public ExecutionCore(
            IJobStore<TJobId, TNodeId, TVersion> jobStore,
            IWorkerPool workerPool,
            IJobExecutor<TJobId, TVersion> jobExecutor,
            ILogger<ExecutionCore<TJobId, TNodeId, TVersion>> logger)
        {
            this.jobStore = jobStore;
            this.workerPool = workerPool;
            this.jobExecutor = jobExecutor;
            this.logger = logger;
        }

        public async Task OnTick(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Starting ExecutionCore iteration. Total {workers} available", this.workerPool.AvailableWorkers);

            if (this.workerPool.AvailableWorkers < 1)
            {
                return;
            }

            var totalJobs = 0;
            await this.jobStore.GetJobsForExecution(this.workerPool.AvailableWorkers, cancellationToken)
                .ForEachAsync(
                    job =>
                        {
                            Interlocked.Increment(ref totalJobs);
                            TaskUtils.FireAndForget(() => this.InvokeJob(job, cancellationToken));
                        },
                    cancellationToken)
                .ConfigureAwait(false);

            this.logger.LogTrace("Total {total} jobs processed at this iteration", totalJobs);
        }

        private async Task InvokeJob(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken)
        {
            try
            {
                await this.InvokeJobCore(jobDescriptor, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                this.logger.LogWarning(ex, "Execution of job {id} cancelled", jobDescriptor.Id);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Execution of job {id} failed", jobDescriptor.Id);
            }
        }

        private async Task InvokeJobCore(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var result = await this.workerPool.InvokeOnPool(() => this.jobExecutor.Invoke(jobDescriptor, cancellationToken), cancellationToken)
                    .ConfigureAwait(false);
                sw.Stop();

                this.logger.LogDebug("Job {id} completed in {elapsed} ({elapsedMs}). Result={result}", jobDescriptor.Id, sw.Elapsed, sw.ElapsedMilliseconds, result);
            }
            catch (TaskRejectedException ex)
            {
                this.logger.LogError(ex, "Job {id} rejected by pool", jobDescriptor.Id);
            }
        }
    }
}