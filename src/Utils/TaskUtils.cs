namespace Lightweight.Scheduler.Utils
{
    using System;
    using System.Threading.Tasks;

    public static class TaskUtils
    {
        public static void FireAndForget(Func<Task> action)
        {
            ForceAsync(action).Forget();
        }

        public static async Task ForceAsync(Func<Task> action)
        {
            await Task.Yield();
            await action().ConfigureAwait(false);
        }
    }
}