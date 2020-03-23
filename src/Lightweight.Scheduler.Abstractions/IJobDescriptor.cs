namespace Lightweight.Scheduler.Abstractions
{
    public interface IJobDescriptor<out TId, TVersion>
    {
        IVersionedId<TId, TVersion> Id { get; }

        IPersistentContext Context { get; }

        string JobClass { get; }

        ISchedule Schedule { get; }
    }
}