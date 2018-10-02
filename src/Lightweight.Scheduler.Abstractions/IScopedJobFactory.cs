namespace Lightweight.Scheduler.Abstractions
{
    using System;

    public interface IScopedJobFactory : IJobFactory
    {
        IDisposable BeginScope();
    }
}
