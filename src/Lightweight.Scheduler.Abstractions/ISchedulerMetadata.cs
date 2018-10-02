namespace Lightweight.Scheduler.Abstractions
{
    using System;

    public interface ISchedulerMetadata : ISchedulerId
    {
        TimeSpan HeartbeatInterval { get; }

        TimeSpan HeartbeatTimeout { get; }
    }
}
