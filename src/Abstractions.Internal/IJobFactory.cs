namespace Lightweight.Scheduler.Abstractions.Internal
{
    public interface IJobFactory<TId>
    {
        IJob Create(IJobDescriptor<TId> jobDescriptor);
    }
}