namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISchedulerMetadataStore<TStorageId>
    {
        Task Heartbeat(ISchedulerId<TStorageId> schedulerId);

        Task<ICollection<(ISchedulerId<TStorageId> id, ISchedulerMetadata metadata)>> GetSchedulers();

        Task RemoveScheduler(ISchedulerId<TStorageId> schedulerId);
    }
}
