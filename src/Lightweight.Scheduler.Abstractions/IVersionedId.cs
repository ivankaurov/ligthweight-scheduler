namespace Lightweight.Scheduler.Abstractions
{
    public interface IVersionedId<out TId, TVersion>
    {
        TId Id { get; }

        TVersion Version { get; set; }
    }
}