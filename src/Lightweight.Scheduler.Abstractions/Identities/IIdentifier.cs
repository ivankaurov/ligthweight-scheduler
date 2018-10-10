namespace Lightweight.Scheduler.Abstractions.Identities
{
    public interface IIdentifier<out TStorageId>
    {
        TStorageId Id { get; }
    }
}
