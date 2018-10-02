namespace Lightweight.Scheduler.Abstractions
{
    using System;
    using System.Collections.Generic;

    public interface IJobMetadata : IJobMetadataId
    {
        Type JobClass { get; }

        IDictionary<string, object> Context { get; }

        TimeSpan? Timeout { get; }

        TimeSpan? Interval { get; }
    }
}
