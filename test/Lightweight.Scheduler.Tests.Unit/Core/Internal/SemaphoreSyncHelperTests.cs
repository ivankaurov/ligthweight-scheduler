namespace Lightweight.Scheduler.Tests.Unit.Core.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Lightweight.Scheduler.Core.Internal;
    using Lightweight.Scheduler.Tests.Unit.Utils;
    using Xunit;

    public class SemaphoreSyncHelperTests
    {
        [Theory]
        [AutoMoqData]
        public async Task ShouldRestrinctConcurrentTasks(int concurrentTasks)
        {
            // Arrange
            ICollection<Task<bool>> tasks;
            using (var sut = new SemaphoreSyncHelper(concurrentTasks))
            {
                // Act
                tasks = Enumerable.Range(0, 2 * concurrentTasks).Select(async _ =>
                {
                    if (await sut.WaitOne(CancellationToken.None))
                    {
                        await Task.Delay(1000);
                        sut.Release();
                        return true;
                    }

                    return false;
                }).ToList();

                await Task.WhenAll(tasks);
            }

            // Assert
            Assert.Equal(concurrentTasks, tasks.Count(t => t.Result));
            Assert.Equal(concurrentTasks, tasks.Count(t => !t.Result));
        }

        [Fact]
        public async Task ShouldCorrectlyRelease()
        {
            // Arrange
            var sut = new SemaphoreSyncHelper(2);
            var taskWithRelease = sut.WaitOne(CancellationToken.None).ContinueWith(async _ =>
            {
                await Task.Delay(500);
                sut.Release();
            });

            await sut.WaitOne(CancellationToken.None);

            // Act
            await taskWithRelease.Unwrap();

            // Assert
            Assert.True(await sut.WaitOne(CancellationToken.None));
            Assert.False(await sut.WaitOne(CancellationToken.None));
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldThrowObjectDisposedException(int concurrentThreads)
        {
            // Arrange
            var sut = new SemaphoreSyncHelper(concurrentThreads);
            sut.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.WaitOne(CancellationToken.None));
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldConsumeCancellationToken(int concurrentThreads)
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                using (var sut = new SemaphoreSyncHelper(concurrentThreads))
                {
                    // Act & Assert
                    Assert.True(await sut.WaitOne(cts.Token));

                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.WaitOne(cts.Token));
                }
            }
        }
    }
}
