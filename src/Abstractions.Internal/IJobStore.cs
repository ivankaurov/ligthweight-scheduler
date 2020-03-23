namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJobStore<TJobId, in TNodeId, TVersion>
    {
        IAsyncEnumerable<IJobDescriptor<TJobId, TVersion>> GetJobsForExecution(int count, CancellationToken cancellationToken);

        Task Put(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken);

        Task StartExclusiveExecution(IVersionedId<TJobId, TVersion> jobId, TNodeId nodeId, TimeSpan? nextExecution, CancellationToken cancellationToken);

        Task StartExecution(IVersionedId<TJobId, TVersion> jobId, TimeSpan? nextExecution, CancellationToken cancellationToken);

        Task CompleteExecution(IJobDescriptor<TJobId, TVersion> jobDescriptor, CancellationToken cancellationToken);
    }
}