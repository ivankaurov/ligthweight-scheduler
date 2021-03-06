﻿namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IClusterStateMonitor<TSchedulerKey>
    {
        Task MonitorClusterState(TSchedulerKey ownerSchedulerId, CancellationToken cancellationToken);
    }
}
