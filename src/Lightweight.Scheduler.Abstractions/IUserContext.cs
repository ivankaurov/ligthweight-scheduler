namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;

    public interface IUserContext
    {
        IDictionary<string, string> Values { get; }
    }
}