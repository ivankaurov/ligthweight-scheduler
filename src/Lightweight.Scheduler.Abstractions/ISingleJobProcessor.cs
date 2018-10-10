namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions.Identities;

    public interface ISingleJobProcessor<TSchedulerKey, TJobKey>
    {
        Task<bool> ProcessSingleJob(
            IIdentifier<TJobKey> jobId,
            IJobMetadata jobMetadata,
            IIdentifier<TSchedulerKey> schedluerId,
            CancellationToken cancellationToken);
    }
}
