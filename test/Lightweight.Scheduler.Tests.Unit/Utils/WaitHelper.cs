namespace Lightweight.Scheduler.Tests.Unit.Utils
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    internal static class WaitHelper
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        public static Task WaitForAction(Func<bool> action)
        {
            return WaitForAction(action, DefaultTimeout);
        }

        public static Task WaitForAction(Func<bool> action, TimeSpan timeout)
        {
            return WaitForAction(() => Task.FromResult(action()), r => r, timeout);
        }

        public static Task<TResult> WaitForAction<TResult>(Func<Task<TResult>> action, Func<TResult, bool> predicate)
        {
            return WaitForAction(action, predicate, DefaultTimeout);
        }

        public static async Task<TResult> WaitForAction<TResult>(Func<Task<TResult>> action, Func<TResult, bool> predicate, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                var res = await action();
                if (predicate(res))
                {
                    return res;
                }

                await Task.Delay(50);
            }
            while (sw.Elapsed < timeout);

            throw new TimeoutException($"Operation failed due timeout of {timeout}");
        }
    }
}
