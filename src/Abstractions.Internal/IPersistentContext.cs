namespace Lightweight.Scheduler.Abstractions.Internal
{
    public interface IPersistentContext
    {
        IUserContext UserContext { get; }

        int Attempt { get; set; }
    }
}