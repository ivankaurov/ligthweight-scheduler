namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions.Identities;

    public interface ISingleJobProcessor<TSchedulerKey, TJobKey>
    {
        Task<bool> ProcessSingleJob(
            IIdentity<TJobKey> jobId,
            IJobMetadata jobMetadata,
            IIdentity<TSchedulerKey> schedluerId,
            CancellationToken cancellationToken);
    }
}
