namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ISchedulerStateMonitor<TSchedulerKey>
    {
        Task MonitorClusterState(IIdentifier<TSchedulerKey> ownerSchedulerId, CancellationToken cancellationToken);
    }
}
