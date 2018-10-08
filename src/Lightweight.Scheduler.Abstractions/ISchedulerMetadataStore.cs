namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISchedulerMetadataStore<TStorageId>
    {
        Task Heartbeat(IIdentifier<TStorageId> schedulerId);

        Task<ICollection<(IIdentifier<TStorageId> id, ISchedulerMetadata metadata)>> GetSchedulers();

        Task RemoveScheduler(IIdentifier<TStorageId> schedulerId);
    }
}
