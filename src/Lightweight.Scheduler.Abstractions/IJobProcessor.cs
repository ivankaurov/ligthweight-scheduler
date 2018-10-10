namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJobProcessor<TSchedulerKey>
    {
        Task ProcessJobs(TSchedulerKey schedulerId, CancellationToken cancellationToken);
    }
}
