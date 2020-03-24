namespace Lightweight.Scheduler.Abstractions
{
    using System;

    public interface ISchedule
    {
        bool Exclusive { get; }

        bool DelayedCalculation { get; }

        TimeSpan? Next(IExecutionContext context);

        TimeSpan? OnError(IExecutionContext context, Exception ex);

        TimeSpan? OnTimeout(IExecutionContext context);
    }
}