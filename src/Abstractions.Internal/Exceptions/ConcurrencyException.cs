namespace Lightweight.Scheduler.Abstractions.Internal.Exceptions
{
    using System;

    public sealed class ConcurrencyException : Exception
    {
        public ConcurrencyException()
            : base("Can't update token due to concurrency error")
        {
        }

        public ConcurrencyException(string message)
            : base(message)
        {
        }

        public ConcurrencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}