namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISchedulerMetadataStore<TStorageId>
    {
        Task Heartbeat(TStorageId schedulerId);

        Task<ICollection<(TStorageId id, ISchedulerMetadata metadata)>> GetSchedulers();

        Task RemoveScheduler(TStorageId schedulerId);
    }
}
