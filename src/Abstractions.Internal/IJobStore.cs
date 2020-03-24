namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJobStore<TJobId, in TNodeId, TVersion>
    {
        IAsyncEnumerable<IJobDescriptor<TJobId, TVersion>> GetJobsForExecution(int count, CancellationToken cancellationToken = default);

        Task Put(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts job execution and locks it with provided <paramref name="nodeId"/>
        /// Next execution time NOT updated.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="nodeId">Node ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async Task.</returns>
        Task StartExclusiveExecution(IVersionedId<TJobId, TVersion> jobId, TNodeId nodeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts job execution, locks it with provided <paramref name="nodeId"/> and updates next execution time to <paramref name="nextExecution"/>.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="nodeId">Node ID.</param>
        /// <param name="nextExecution">Delta to next execution.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async task.</returns>
        Task StartExclusiveExecution(IVersionedId<TJobId, TVersion> jobId, TNodeId nodeId, TimeSpan? nextExecution, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts job execution without locking it.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="nodeId">Node ID.</param>
        /// <param name="nextExecution">Next execution time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async task.</returns>
        Task StartExecution(IVersionedId<TJobId, TVersion> jobId, TNodeId nodeId, TimeSpan? nextExecution, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes job execution and unlocks it if necessary.
        /// Next execution time NOT updated.
        /// </summary>
        /// <param name="jobDescriptor">Job descriptor.</param>
        /// <param name="result">Job execution result.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async Task.</returns>
        Task CompleteExecution(IJobDescriptor<TJobId, TVersion> jobDescriptor, JobExecutionResult result, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes job execution, unlocks job if necessary and updates next execution time.
        /// </summary>
        /// <param name="jobDescriptor">Job descriptor.</param>
        /// <param name="result">Job execution result.</param>
        /// <param name="nextExecution">Next execution time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async task.</returns>
        Task CompleteExecution(IJobDescriptor<TJobId, TVersion> jobDescriptor, JobExecutionResult result, TimeSpan? nextExecution, CancellationToken cancellationToken = default);
    }
}