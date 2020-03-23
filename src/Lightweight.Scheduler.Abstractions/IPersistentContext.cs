namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;

    public interface IPersistentContext
    {
        IDictionary<string, string> Values { get; }
    }
}