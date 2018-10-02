namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading.Tasks;

    public interface IScheduler : ISchedulerMetadata
    {
        Task Start();

        Task Stop();
    }
}
