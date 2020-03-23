namespace Lightweight.Scheduler.Core.Configuration
{
    using System;
    using System.Collections.Generic;

    using Lightweight.Scheduler.Utils;

    internal sealed class MainLoopOptionsValidator : ValidateOptionsBase<MainLoopOptions>
    {
        protected override IEnumerable<string> ValidateInternal(MainLoopOptions options)
        {
            if (options.LoopFrequency <= TimeSpan.Zero)
            {
                yield return nameof(options.LoopFrequency) + " should be positive";
            }
        }
    }
}