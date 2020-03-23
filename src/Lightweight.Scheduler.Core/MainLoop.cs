namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Lightweight.Scheduler.Abstractions.Internal;
    using Lightweight.Scheduler.Core.Configuration;
    using Lightweight.Scheduler.Utils;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal sealed class MainLoop
    {
        private readonly IReadOnlyCollection<ITickHandler> tickHandlers;

        private readonly ILogger<MainLoop> logger;

        private readonly MainLoopOptions options;

        public MainLoop(IEnumerable<ITickHandler> tickHandlers, IOptions<MainLoopOptions> options, ILogger<MainLoop> logger)
        {
            this.options = options.Value;
            this.tickHandlers = tickHandlers.AsReadOnlyCollection();
            this.logger = logger;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Main loop starting");

            while (!cancellationToken.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                await Task.WhenAll(this.tickHandlers.Select(handler => this.SecureHandleTick(handler, cancellationToken))).ConfigureAwait(false);
                sw.Stop();

                if (sw.Elapsed < this.options.LoopFrequency)
                {
                    try
                    {
                        await Task.Delay(this.options.LoopFrequency - sw.Elapsed, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            this.logger.LogInformation("Main loop completed");
        }

        private async Task SecureHandleTick(ITickHandler tickHandler, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await tickHandler.OnTick(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                this.logger.LogTrace(
                    "TickHandler {tickHandler} successfully completed in {elapsed} ms",
                    tickHandler,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                this.logger.LogWarning(ex, "TickHandler {tickHandler} cancelled after {elapsed} ms", tickHandler, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                this.logger.LogError(ex, "TickHandler {tickHandler} failed after {elapsed} ms", tickHandler, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}