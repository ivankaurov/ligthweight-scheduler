namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJobExecutor<in TId, TVersion>
    {
        Task Invoke(IJobDescriptor<TId, TVersion> jobDescriptor, CancellationToken cancellationToken);
    }
}