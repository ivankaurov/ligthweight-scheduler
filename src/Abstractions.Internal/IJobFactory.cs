namespace Lightweight.Scheduler.Abstractions.Internal
{
    public interface IJobFactory<in TId, TVersion>
    {
        IJob Create(IJobDescriptor<TId, TVersion> jobDescriptor);
    }
}