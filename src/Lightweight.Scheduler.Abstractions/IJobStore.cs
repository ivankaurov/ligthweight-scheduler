namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IJobStore<TJobKey, TSchedulerKey>
    {
        Task ScheduleJob(IIdentifier<TJobKey> jobId, IJobMetadata jobMetadata);

        Task<ICollection<(IIdentifier<TJobKey> id, IJobMetadata metadata)>> GetJobsForExecution();

        Task SetJobOwner(IIdentifier<TJobKey> jobMetadataId, IIdentifier<TSchedulerKey> schedulerId);

        Task ClearJobOwner(IIdentifier<TJobKey> jobMetadataId);
    }
}
