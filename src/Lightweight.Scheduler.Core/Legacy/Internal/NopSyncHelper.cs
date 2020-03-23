namespace Lightweight.Scheduler.Core.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class NopSyncHelper : ISyncHelper
    {
        public void Release()
        {
        }

        public Task<bool> WaitOne(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }
    }
}
