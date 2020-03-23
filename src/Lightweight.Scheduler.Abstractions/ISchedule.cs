namespace Lightweight.Scheduler.Abstractions
{
    using System;

    public interface ISchedule
    {
        bool Exclusive { get; }

        bool Next(IExecutionContext context, out TimeSpan nextDelta);

        bool OnError(IExecutionContext context, Exception ex, out TimeSpan nextDelta);

        bool OnTimeout(IExecutionContext context, out TimeSpan nextDelta);
    }
}