namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJob
    {
        Task Invoke(IJobMetadata jobMetadata, CancellationToken cancellationToken);
    }
}
