namespace Lightweight.Scheduler.Abstractions
{
    public interface IVersionedId
    {
        string Id { get; }

        long Version { get; set; }
    }
}
