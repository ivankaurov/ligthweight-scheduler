namespace Lightweight.Scheduler.Core.JobMetadata
{
    using System;
    using System.Collections.Generic;
    using Lightweight.Scheduler.Abstractions;

    public class RecurrentJobMetadata : IJobMetadata
    {
        public Type JobClass { get; set; }

        public IDictionary<string, object> Context { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public TimeSpan? Timeout { get; set; }

        public TimeSpan Interval { get; set; } = TimeSpan.Zero;

        public virtual DateTimeOffset? NextExecution { get; set; }

        public virtual void SetNextExecutionTime()
        {
            this.NextExecution = DateTimeOffset.UtcNow.Add(this.Interval);
        }
    }
}
