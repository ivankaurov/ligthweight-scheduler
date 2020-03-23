namespace Lightweight.Scheduler.Abstractions.Legacy
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJob
    {
        Task Invoke(IJobMetadata jobMetadata, CancellationToken cancellationToken);
    }
}
