namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IJobStore<TJobKey, TSchedulerKey>
    {
        Task ScheduleJob(IIdentifier<TJobKey> jobId, IJobMetadata jobMetadata);

        Task<ICollection<(IIdentifier<TJobKey> id, IJobMetadata metadata)>> GetJobsForExecution();

        Task SetJobOwner(IIdentifier<TJobKey> jobId, IIdentifier<TSchedulerKey> schedulerId);

        Task UpdateJob(IIdentifier<TJobKey> jobId, IJobMetadata metadata, IIdentifier<TSchedulerKey> schedulerId);
    }
}
