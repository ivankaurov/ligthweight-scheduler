namespace Lightweight.Scheduler.Abstractions
{
    public interface IIdentifier<out TStorageId>
    {
        TStorageId Id { get; }
    }
}
