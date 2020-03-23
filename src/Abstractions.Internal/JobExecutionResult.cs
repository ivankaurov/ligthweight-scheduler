namespace Lightweight.Scheduler.Abstractions.Internal
{
    public enum JobExecutionResult
    {
        NotStarted,
        Cancelled,
        Timeouted,
        Failed,
        Succeeded,
    }
}