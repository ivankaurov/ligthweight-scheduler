namespace Lightweight.Scheduler.Core.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    internal interface ISyncHelper
    {
        Task<bool> WaitOne(CancellationToken cancellationToken);

        void Release();
    }
}
