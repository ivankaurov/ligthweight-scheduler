namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Lightweight.Scheduler.Abstractions.Identities;

    public interface ISchedulerMetadataStore<TStorageId>
    {
        Task Heartbeat(IIdentity<TStorageId> schedulerId);

        Task<ICollection<(IIdentity<TStorageId> id, ISchedulerMetadata metadata)>> GetSchedulers();

        Task RemoveScheduler(IIdentity<TStorageId> schedulerId);
    }
}
