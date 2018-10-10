namespace Lightweight.Scheduler.Abstractions
{
    using System;

    public interface IDateTimeProvider
    {
        DateTimeOffset Now();
    }
}
