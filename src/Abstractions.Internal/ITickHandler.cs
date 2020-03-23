namespace Lightweight.Scheduler.Abstractions.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITickHandler
    {
        Task OnTick(CancellationToken cancellationToken);
    }
}