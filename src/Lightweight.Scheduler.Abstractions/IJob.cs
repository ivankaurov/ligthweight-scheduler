namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJob
    {
        Task Invoke(IDictionary<string, object> jobContext, CancellationToken cancellationToken);
    }
}
