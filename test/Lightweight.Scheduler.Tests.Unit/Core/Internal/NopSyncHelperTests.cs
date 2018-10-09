namespace Lightweight.Scheduler.Tests.Unit.Core.Internal
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Core.Internal;
    using Lightweight.Scheduler.Tests.Unit.Internal;
    using Xunit;

    public class NopSyncHelperTests
    {
        [Theory]
        [AutoMoqData]
        public async Task ShouldAllowAnyTasksNumberToRun(int tasksCount)
        {
            // Arrange
            var sut = new NopSyncHelper();
            var runningTasks = 0;

            // Act
            var tasks = Enumerable.Range(0, tasksCount).AsParallel().Select(async _ =>
             {
                 await sut.WaitOne(CancellationToken.None);
                 Interlocked.Increment(ref runningTasks);
             }).ToList();

            await Task.WhenAll(tasks);

            // Assert
            Assert.All(tasks, t => Assert.Equal(TaskStatus.RanToCompletion, t.Status));
            Assert.Equal(tasksCount, runningTasks);
        }

        [Fact]
        public async Task ShouldConsumeCancellationToken()
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                var sut = new NopSyncHelper();

                // Act & Assert
                Assert.True(await sut.WaitOne(cts.Token));

                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.WaitOne(cts.Token));
            }
        }
    }
}
