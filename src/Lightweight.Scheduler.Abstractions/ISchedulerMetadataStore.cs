namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISchedulerMetadataStore
    {
        Task Heartbeat(ISchedulerId schedulerId);

        Task<ICollection<ISchedulerMetadata>> GetSchedulers();

        Task RemoveScheduler(ISchedulerId schedulerId);
    }
}
