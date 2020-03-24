// -----------------------------------------------------------------------
// <copyright file="CoreJobExecutor.cs" company="Intermedia">
//   Copyright © Intermedia.net, Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

        private readonly IJobFactory<TJobId, TVersion> jobFactory;

        private readonly IJobStore<TJobId, TNodeId, TVersion> jobStore;

        private readonly InternalJobExecutor<TJobId, TVersion> internalJobExecutor;

        private readonly ILogger<CoreJobExecutor<TJobId, TNodeId, TVersion>> logger;

        public CoreJobExecutor(
            TNodeId ownerNodeId,
            IJobFactory<TJobId, TVersion> jobFactory,
            IJobStore<TJobId, TNodeId, TVersion> jobStore,
            InternalJobExecutor<TJobId, TVersion> internalJobExecutor,
            ILogger<CoreJobExecutor<TJobId, TNodeId, TVersion>> logger)
        {
            this.ownerNodeId = ownerNodeId;
            this.jobFactory = jobFactory;
            this.jobStore = jobStore;
            this.internalJobExecutor = internalJobExecutor;
            this.logger = logger;
        }

        public async Task<JobExecutionResult> Invoke(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken)
        {
            var context = new ExecutionContext(jobDescriptor.Context.UserContext, jobDescriptor.Context.Attempt + 1);
            this.logger.LogTrace("Trying to start execution of job {id} attempt {attempt}", jobDescriptor.Id, context.Attempt);
            var nextDelta = this.GetNextExecutionDelta(jobDescriptor, context);

            try
            {
                await this.StartJobExecution(jobDescriptor, nextDelta, cancellationToken);
            }
            catch (ConcurrencyException ex)
            {
                this.logger.LogDebug(ex, "Job {id} should be executing on another node", jobDescriptor.Id);
                return JobExecutionResult.NotStarted;
            }

            (JobExecutionResult Result, TimeSpan? NextDelta) result = (JobExecutionResult.Failed, null);
            try
            {
                result = await this.InvokeCore(jobDescriptor, context).ConfigureAwait(false);
            }
            finally
            {
                await this.CompleteJobExecution(jobDescriptor, context, result.Result, result.NextDelta, cancellationToken);
            }

            return result.Result;
        }

        private async Task StartJobExecution(IJobDescriptor<TJobId, TVersion> jobDescriptor, TimeSpan? nextDelta, CancellationToken cancellationToken)
        {
            if (jobDescriptor.Schedule.Exclusive)
            {
                await this.jobStore.StartExclusiveExecution(jobDescriptor.Id, this.ownerNodeId, nextDelta, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await this.jobStore.StartExecution(jobDescriptor.Id, nextDelta, cancellationToken)
                    .ConfigureAwait(false);
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

        private async Task<(JobExecutionResult result, TimeSpan? NextDelta)> InvokeCore(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            IExecutionContext context)
        {
            var result = try
        }

        private async Task CompleteJobExecution(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            IExecutionContext executionContext,
            JobExecutionResult result,
            TimeSpan? nextExecution,
            CancellationToken cancellationToken)
        {
            jobDescriptor.Context.Attempt = result == JobExecutionResult.Succeeded ? 0 : executionContext.Attempt;
            try
            {
                await this.jobStore.CompleteExecution(jobDescriptor, result, nextExecution, cancellationToken)
                    .ConfigureAwait(false);
                this.logger.LogTrace(
                    "Completion information of job {id}: {result} Delta: {delta} stored successfully",
                    jobDescriptor.Id,
                    result,
                    nextExecution);
            }
            catch (Exception ex)
            {
                this.logger.LogCritical(ex, "Failed to complete job {id}: {result} Delta: {delta}", jobDescriptor.Id, result, nextExecution);
            }
        }
    }
}