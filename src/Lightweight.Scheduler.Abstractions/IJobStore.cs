﻿namespace Lightweight.Scheduler.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions.Identities;

    public interface IJobStore<TJobKey, TSchedulerKey>
    {
        Task<ICollection<(IIdentifier<TJobKey> id, IJobMetadata metadata)>> GetJobsForExecution();

        Task SetJobOwner(IIdentifier<TJobKey> jobId, IIdentifier<TSchedulerKey> schedulerId);

        Task UpdateJob(IIdentifier<TJobKey> jobId, IJobMetadata metadata, IIdentifier<TSchedulerKey> schedulerId);
    }
}
