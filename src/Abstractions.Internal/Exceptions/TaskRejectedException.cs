namespace Lightweight.Scheduler.Abstractions.Internal.Exceptions
{
    using System;

    public sealed class TaskRejectedException : Exception
    {
        public TaskRejectedException()
            : base("Task execution rejected due to pool overload")
        {
        }

        public TaskRejectedException(string message)
            : base(message)
        {
        }

        public TaskRejectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}