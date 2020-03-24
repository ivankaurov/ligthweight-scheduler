// -----------------------------------------------------------------------
// <copyright file="InternalJobExecutor.cs" company="Intermedia">
//   Copyright © Intermedia.net, Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Lightweight.Scheduler.Core.Execution
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Abstractions.Internal;

    internal sealed class InternalJobExecutor<TJobId, TVersion>
    {
        public Task Invoke(
            IJobDescriptor<TJobId, TVersion> jobDescriptor,
            IExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}