namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions.Identities;

    public interface ISchedulerStateMonitor<TSchedulerKey>
    {
        Task MonitorClusterState(IIdentifier<TSchedulerKey> ownerSchedulerId, CancellationToken cancellationToken);
    }
}
