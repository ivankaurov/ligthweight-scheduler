namespace Lightweight.Scheduler.Abstractions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJob
    {
        Task Invoke(IExecutionContext context, CancellationToken cancellationToken);

        Task OnError(IExecutionContext context, Exception ex, CancellationToken cancellationToken);

        Task OnTimeout(IExecutionContext context, CancellationToken cancellationToken);
    }
}