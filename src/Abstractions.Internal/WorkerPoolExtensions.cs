namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class WorkerPoolExtensions
    {
        public static Task InvokeOnPool(this IWorkerPool workerPool, Func<Task> action, CancellationToken cancellationToken)
        {
            return workerPool.InvokeOnPool(
                async () =>
                    {
                        await action().ConfigureAwait(false);
                        return true;
                    },
                cancellationToken);
        }
    }
}