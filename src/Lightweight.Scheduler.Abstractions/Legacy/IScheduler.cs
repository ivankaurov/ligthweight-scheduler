namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading.Tasks;

    public interface IScheduler
    {
        Task Start();

        Task Stop();
    }
}
