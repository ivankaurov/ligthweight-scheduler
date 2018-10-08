namespace Lightweight.Scheduler.Abstractions.Exceptions
{
    using System;

    public class ConcurrencyException : Exception
    {
        public ConcurrencyException()
            : base("Item has already been updated")
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
