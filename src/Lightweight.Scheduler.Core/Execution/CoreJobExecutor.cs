namespace Lightweight.Scheduler.Core.Execution
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Abstractions.Internal;
    using Lightweight.Scheduler.Abstractions.Internal.Exceptions;

    using Microsoft.Extensions.Logging;

    internal sealed class CoreJobExecutor<TJobId, TNodeId, TVersion> : IJobExecutor<TJobId, TVersion>
    {
        private readonly TNodeId ownerNodeId;

        private readonly IJobStore<TJobId, TNodeId, TVersion> jobStore;

        private readonly InternalJobExecutor<TJobId, TVersion> internalJobExecutor;

        private readonly ILogger<CoreJobExecutor<TJobId, TNodeId, TVersion>> logger;

        public CoreJobExecutor(
            TNodeId ownerNodeId,
            IJobStore<TJobId, TNodeId, TVersion> jobStore,
            InternalJobExecutor<TJobId, TVersion> internalJobExecutor,
            ILogger<CoreJobExecutor<TJobId, TNodeId, TVersion>> logger)
        {
            this.ownerNodeId = ownerNodeId;
            this.jobStore = jobStore;
            this.internalJobExecutor = internalJobExecutor;
            this.logger = logger;
        }

        public async Task<JobExecutionResult> Invoke(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Trying to start execution of job {id}", jobDescriptor.Id);

            IExecutionContext executionContext;
            try
            {
                executionContext = await this.StartJobExecution(jobDescriptor, cancellationToken).ConfigureAwait(false);
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogDebug(ex, "Job {id} should be executing on another node", jobDescriptor.Id);
                return JobExecutionResult.NotStarted;
            }

            return await this.InvokeCore(jobDescriptor, executionContext, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IExecutionContext> StartJobExecution(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken)
        {
            var executionContext = new ExecutionContext(jobDescriptor.Context.UserContext, jobDescriptor.Context.Attempt + 1);

            switch (jobDescriptor.Schedule)
            {
                case { Exclusive: true, DelayedCalculation: true }:
                    await this.jobStore.StartExclusiveExecution(jobDescriptor.Id, this.ownerNodeId, cancellationToken).ConfigureAwait(false);
                    return executionContext;

                case { Exclusive: true, DelayedCalculation: false }:
                    await this.jobStore.StartExclusiveExecution(
                        jobDescriptor.Id,
                        this.ownerNodeId,
                        this.GetNextExecutionDelta(jobDescriptor, executionContext),
                        cancellationToken);
                    return executionContext;

                case { Exclusive: false, DelayedCalculation: false }:
                    await this.jobStore.StartExecution(
                        jobDescriptor.Id,
                        this.ownerNodeId,
                        this.GetNextExecutionDelta(jobDescriptor, executionContext),
                        cancellationToken);
                    return executionContext;

                default:
                    // We should always provide next execution time on job start on non-exclusive jobs.
                    throw new NotSupportedException(
                        $"Schedule for job {jobDescriptor.Id} of Exclusive={jobDescriptor.Schedule.Exclusive} and Delayed={jobDescriptor.Schedule.DelayedCalculation} not supported");
            }
        }

        private TimeSpan? GetNextExecutionDelta(IJobDescriptor<TJobId, TVersion> jobDescriptor, IExecutionContext context)
        {
            try
            {
                return jobDescriptor.Schedule.Next(context);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to calculate next delta for job {id}. Please verify next execution manually", jobDescriptor.Id);
                throw;
            }
        }

        private async Task<JobExecutionResult> InvokeCore(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            IExecutionContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                await this.internalJobExecutor.Invoke(jobDescriptor, context, cancellationToken).ConfigureAwait(false);
                await this.HandleJobCompletion(jobDescriptor, context).ConfigureAwait(false);
                return JobExecutionResult.Succeeded;
            }
            catch (TimeoutException ex)
            {
                await this.HandleJobTimeout(jobDescriptor, ex, context).ConfigureAwait(false);
                return JobExecutionResult.Succeeded;
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                await this.HandleJobCancellation(jobDescriptor, ex, context).ConfigureAwait(false);
                return JobExecutionResult.Cancelled;
            }
            catch (Exception ex)
            {
                await this.HandleJobFailure(jobDescriptor, ex, context).ConfigureAwait(false);
                return JobExecutionResult.Failed;
            }
        }

        private async Task HandleJobCompletion(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            IExecutionContext executionContext)
        {
            this.logger.LogInformation("Job {id} completed at attempt {attempt}", executionContext.Attempt);
            jobDescriptor.Context.Attempt = 0;

            await this.CompleteJobExecution(
                jobDescriptor,
                JobExecutionResult.Succeeded,
                jobDescriptor.Schedule.DelayedCalculation ? new Func<TimeSpan?>(() => jobDescriptor.Schedule.Next(executionContext)) : null);
        }

        private async Task HandleJobTimeout(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            TimeoutException ex,
            IExecutionContext executionContext)
        {
            this.logger.LogWarning(ex, "Execution of job {id} lead to timeout at attempt", jobDescriptor.Id, executionContext.Attempt);

            await this.CompleteJobExecution(
                jobDescriptor,
                JobExecutionResult.Timeouted,
                jobDescriptor.Schedule.Exclusive ? new Func<TimeSpan?>(() => jobDescriptor.Schedule.OnTimeout(executionContext)) : null);
        }

        private async Task HandleJobCancellation(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            OperationCanceledException ex,
            IExecutionContext executionContext)
        {
            this.logger.LogWarning(ex, "Execution of job {id} cancelled due to service stop at attempt", jobDescriptor.Id, executionContext.Attempt);

            await this.CompleteJobExecution(jobDescriptor, JobExecutionResult.Cancelled).ConfigureAwait(false);
        }

        private async Task HandleJobFailure(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            Exception ex,
            IExecutionContext executionContext)
        {
            this.logger.LogError(ex, "Execution of job {id} failed at attempt", jobDescriptor.Id, executionContext.Attempt);

            await this.CompleteJobExecution(
                    jobDescriptor,
                    JobExecutionResult.Failed,
                    jobDescriptor.Schedule.Exclusive ? new Func<TimeSpan?>(() => jobDescriptor.Schedule.OnError(executionContext, ex)) : null)
                .ConfigureAwait(false);
        }

        private async Task CompleteJobExecution(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            JobExecutionResult result,
            Func<TimeSpan?>? calculateDelta)
        {
            if (calculateDelta != null)
            {
                TimeSpan? nextDelta;
                try
                {
                    nextDelta = calculateDelta();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to calculate text delta. Job will be rescheduled immediately");
                    await this.CompleteJobExecution(jobDescriptor, result).ConfigureAwait(false);
                    return;
                }

                await this.CompleteJobExecution(jobDescriptor, result, nextDelta).ConfigureAwait(false);
            }
            else
            {
                await this.CompleteJobExecution(jobDescriptor, result).ConfigureAwait(false);
            }
        }

        private async Task CompleteJobExecution(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            JobExecutionResult result,
            TimeSpan? nextDelta)
        {
            this.logger.LogInformation(
                "Job {id} execution finished: {result}. Next time job will be executed in {nextDelta}",
                jobDescriptor.Id,
                result,
                nextDelta);

            try
            {
                await this.jobStore.CompleteExecution(jobDescriptor, result, nextDelta).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogCritical(
                    ex,
                    "Failed to complete job execution {id} and set next delta to {nextDelta}. Please verify job state manually",
                    jobDescriptor.Id,
                    nextDelta);
            }
        }

        private async Task CompleteJobExecution(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            JobExecutionResult result)
        {
            try
            {
                await this.jobStore.CompleteExecution(jobDescriptor, result)
                    .ConfigureAwait(false);
            }
            catch (ConcurrencyException ex) when (jobDescriptor.Schedule.Exclusive)
            {
                this.logger.LogCritical(
                    ex,
                    "Failed to free exclusively locked job {id} because it has been updated by someone else. Please verify job state manually",
                    jobDescriptor.Id);
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogInformation(
                    ex,
                    "Failed to complete non-exclusive job {id}.It means that changed context hasn't been persisted",
                    jobDescriptor.Id);
            }
            catch (Exception ex) when (jobDescriptor.Schedule.Exclusive)
            {
                this.logger.LogCritical(ex, "Failed to free exclusively locked job {id}. Please verify job state manually", jobDescriptor.Id);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to store persistent context for job {id}", jobDescriptor.Id);
            }
        }
    }
}