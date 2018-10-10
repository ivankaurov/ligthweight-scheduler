namespace Lightweight.Scheduler.Abstractions
{
    public enum JobExecutionResult
    {
        NotStarted,
        Succeeded,
        Cancelled,
        Timeouted,
        Failed,
    }
}
