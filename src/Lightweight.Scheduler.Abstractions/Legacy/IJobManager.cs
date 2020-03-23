namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading.Tasks;

    public interface IJobManager<TJobKey>
    {
        Task ScheduleJob(TJobKey jobId, IJobMetadata jobMetadata);

        Task<bool> RemoveJob(TJobKey jobId);

        Task PauseJob(TJobKey jobId);

        Task ResumeJob(TJobKey jobId);

        Task<IJobMetadata> GetJob(TJobKey jobId);
    }
}
