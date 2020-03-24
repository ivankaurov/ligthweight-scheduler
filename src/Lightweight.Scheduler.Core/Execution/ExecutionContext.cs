﻿namespace Lightweight.Scheduler.Core.Execution
{
    using Lightweight.Scheduler.Abstractions;

    internal sealed class ExecutionContext : IExecutionContext
    {
        public ExecutionContext(IUserContext userContext, int attempt)
        {
            this.UserContext = userContext;
            this.Attempt = attempt;
        }

        public IUserContext UserContext { get; }

        public int Attempt { get; }
    }
}