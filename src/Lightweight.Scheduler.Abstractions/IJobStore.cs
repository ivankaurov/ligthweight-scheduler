namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IJobStore<TJobKey, TSchedulerKey>
    {
        Task<ICollection<(TJobKey id, IJobMetadata metadata)>> GetExecutingJobs(TSchedulerKey schedulerId);

        Task<ICollection<(TJobKey id, IJobMetadata metadata)>> GetTimeoutedJobs();

        Task<ICollection<(TJobKey id, IJobMetadata metadata)>> GetJobsForExecution();

        Task SetJobOwner(TJobKey jobId, TSchedulerKey schedulerId);

        Task FinalizeJob(TJobKey jobId, IJobMetadata metadata, JobExecutionResult result);

        Task RecoverJob(TJobKey jobId, JobExecutionResult recoverReason);
    }
}
