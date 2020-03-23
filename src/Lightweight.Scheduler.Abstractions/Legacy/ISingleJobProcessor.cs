namespace Lightweight.Scheduler.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ISingleJobProcessor<TSchedulerKey, TJobKey>
    {
        Task<JobExecutionResult> ProcessSingleJob(
            TJobKey jobId,
            IJobMetadata jobMetadata,
            TSchedulerKey schedluerId,
            CancellationToken cancellationToken);
    }
}
