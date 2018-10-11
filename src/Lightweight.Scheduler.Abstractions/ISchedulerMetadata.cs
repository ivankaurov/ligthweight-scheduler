namespace Lightweight.Scheduler.Abstractions
{
    using System;

    public interface ISchedulerMetadata
    {
        TimeSpan HeartbeatInterval { get; }

        TimeSpan HeartbeatTimeout { get; }

        DateTimeOffset LastCheckin { get; }
    }
}
