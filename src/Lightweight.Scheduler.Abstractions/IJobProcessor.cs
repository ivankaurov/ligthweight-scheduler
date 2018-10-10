namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions.Identities;

    public interface IJobProcessor<TSchedulerKey>
    {
        Task ProcessJobs(IIdentity<TSchedulerKey> schedulerId, CancellationToken cancellationToken);
    }
}
