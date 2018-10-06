namespace Lightweight.Scheduler.Abstractions
{
    using System;

    public interface IJobFactory
    {
        IJob CreateJobInstance(IJobMetadata jobMetadata);

        IDisposable BeginScope();
    }
}
