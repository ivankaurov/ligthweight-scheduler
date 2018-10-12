namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISchedulerMetadataStore<TStorageId>
    {
        Task AddScheduler(TStorageId schedulerId, ISchedulerMetadata schedulerMetadata);

        Task Heartbeat(TStorageId schedulerId);

        Task<ICollection<(TStorageId id, ISchedulerMetadata metadata)>> GetStalledSchedulers();

        Task RemoveScheduler(TStorageId schedulerId);
    }
}
