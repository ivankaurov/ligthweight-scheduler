namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IWorkerPool
    {
        int AvailableWorkers { get; }

        Task InvokeOnPool(Func<Task> action, CancellationToken cancellationToken);
    }
}