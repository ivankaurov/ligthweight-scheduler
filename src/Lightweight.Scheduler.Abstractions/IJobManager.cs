namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions.Identities;

    public interface IJobManager<TJobKey>
    {
        Task ScheduleJob(IIdentity<TJobKey> jobId, IJobMetadata jobMetadata);

        Task<bool> RemoveJob(IIdentity<TJobKey> jobId);

        Task PauseJob(IIdentity<TJobKey> jobId);

        Task ResumeJob(IIdentity<TJobKey> jobId);

        Task<IJobMetadata> GetJob(IIdentity<TJobKey> jobId);
    }
}
