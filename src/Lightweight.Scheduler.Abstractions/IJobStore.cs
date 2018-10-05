namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IJobStore<TJobKey, TSchedulerKey>
    {
        Task ScheduleJob(IJobMetadataId<TJobKey> jobId, IJobMetadata jobMetadata);

        Task<ICollection<(IJobMetadataId<TJobKey> id, IJobMetadata metadata)>> GetJobsForExecution();

        Task SetJobOwner(IJobMetadataId<TJobKey> jobMetadataId, ISchedulerId<TSchedulerKey> schedulerId);

        Task ClearJobOwner(IJobMetadataId<TJobKey> jobMetadataId);
    }
}
