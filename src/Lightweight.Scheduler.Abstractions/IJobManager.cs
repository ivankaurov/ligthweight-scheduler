namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading.Tasks;

    public interface IJobManager<TJobKey>
    {
        Task ScheduleJob(IIdentifier<TJobKey> jobId, IJobMetadata jobMetadata);

        Task<bool> RemoveJob(IIdentifier<TJobKey> jobId);

        Task PauseJob(IIdentifier<TJobKey> jobId);

        Task ResumeJob(IIdentifier<TJobKey> jobId);

        Task<IJobMetadata> GetJob(IIdentifier<TJobKey> jobId);
    }
}
