namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IWorker
    {
        Task<JobExecutionResult> Invoke(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
    }
}