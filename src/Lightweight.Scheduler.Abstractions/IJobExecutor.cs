namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJobExecutor
    {
        Task StartJobExecution(IJobMetadata jobMetadata, ISchedulerId schedulerId, CancellationToken cancellationToken);
    }
}
