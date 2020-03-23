namespace Lightweight.Scheduler.Abstractions
{
    public interface IJobDescriptor<TId>
    {
        TId Id { get; }

        IPersistentContext Context { get; }

        string JobClass { get; }

        ISchedule Schedule { get; }
    }
}