namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IJobStore
    {
        Task ScheduleJob(IJobMetadata jobMetadata, ISchedulerId schedulerId);

        Task<ICollection<IJobMetadata>> GetJobsForExecution();

        Task SetJobOwner(IJobMetadataId jobMetadataId, ISchedulerId schedulerId);

        Task ClearJobOwner(IJobMetadataId jobMetadataId);
    }
}
