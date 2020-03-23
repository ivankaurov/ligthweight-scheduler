namespace Lightweight.Scheduler.Abstractions
{
    public sealed class VersionedId<TId, TVersion> : IVersionedId<TId, TVersion>
    {
        public VersionedId(TId id, TVersion version = default)
        {
            this.Id = id;
            this.Version = version;
        }

        public TId Id { get; }

        public TVersion Version { get; set; }
    }
}