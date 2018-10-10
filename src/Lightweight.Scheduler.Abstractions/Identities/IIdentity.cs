namespace Lightweight.Scheduler.Abstractions.Identities
{
    public interface IIdentity<out TStorageId>
    {
        TStorageId Id { get; }
    }
}
