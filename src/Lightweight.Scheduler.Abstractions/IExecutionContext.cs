namespace Lightweight.Scheduler.Abstractions
{
    public interface IExecutionContext
    {
        IPersistentContext PersistentContext { get; }

        int Attempt { get; }
    }
}