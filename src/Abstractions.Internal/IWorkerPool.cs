namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IWorkerPool
    {
        int AvailableWorkers { get; }

        Task<IWorker?> TryGetWorker(CancellationToken cancellationToken);
    }
}