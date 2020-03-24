// -----------------------------------------------------------------------
// <copyright file="JobExecutionResult.cs" company="Intermedia">
//   Copyright © Intermedia.net, Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Lightweight.Scheduler.Abstractions.Internal
{
    public enum JobExecutionResult
    {
        NotStarted,
        Failed,
        Cancelled,
        Timeouted,
        Succeeded,
    }
}