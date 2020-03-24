namespace Lightweight.Scheduler.Abstractions.Internal
{
    public enum JobExecutionResult
    {
        NotStarted,
        Failed,
        Cancelled,
        Timeouted,
        Succeeded,
    }
}