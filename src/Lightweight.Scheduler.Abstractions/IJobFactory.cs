namespace Lightweight.Scheduler.Abstractions
{
    public interface IJobFactory
    {
        IJob CreateJobInstance(IJobMetadata jobMetadata);
    }
}
