namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;

    public interface IExecutionContext
    {
        IPersistentContext PersistentContext { get; }

        int Attempt { get; }
    }
}