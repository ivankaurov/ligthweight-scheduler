namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJobExecutor
    {
        Task InvokeJob(IJobMetadata jobMetadata, CancellationToken cancellationToken);
    }
}
