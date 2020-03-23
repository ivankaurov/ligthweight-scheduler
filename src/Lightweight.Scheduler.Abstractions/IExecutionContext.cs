namespace Lightweight.Scheduler.Abstractions
{
    public interface IExecutionContext
    {
        IUserContext UserContext { get; }

        int Attempt { get; }
    }
}