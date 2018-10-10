namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions.Identities;

    public interface IJobStore<TJobKey, TSchedulerKey>
    {
        Task<ICollection<(IIdentity<TJobKey> id, IJobMetadata metadata)>> GetJobsForExecution();

        Task SetJobOwner(IIdentity<TJobKey> jobId, IIdentity<TSchedulerKey> schedulerId);

        Task UpdateJob(IIdentity<TJobKey> jobId, IJobMetadata metadata, IIdentity<TSchedulerKey> schedulerId);
    }
}
