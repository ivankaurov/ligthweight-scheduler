using System;

namespace Lightweight.Scheduler.Abstractions
{
    public interface ISchedulerMetadata : ISchedulerId
    {
        TimeSpan HeartbeatInterval { get; }

        TimeSpan HeartbeatTimeout { get; }
    }
}
