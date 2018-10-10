namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Core;
    using Lightweight.Scheduler.Tests.Unit.Utils;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class DefaultSchedulerTests
    {
        private readonly Guid schedulerId = Guid.NewGuid();
        private readonly Mock<ISchedulerMetadata> schedulerMetadata = new Mock<ISchedulerMetadata>();
        private readonly Mock<ISchedulerMetadataStore<Guid>> schedulerMetadataStore = new Mock<ISchedulerMetadataStore<Guid>>();
        private readonly Mock<ISchedulerStateMonitor<Guid>> schedulerStateMonitor = new Mock<ISchedulerStateMonitor<Guid>>();
        private readonly Mock<IJobProcessor<Guid>> jobProcessor = new Mock<IJobProcessor<Guid>>();
        private readonly Mock<ILogger<DefaultScheduler<Guid>>> logger = new Mock<ILogger<DefaultScheduler<Guid>>>();
        private readonly DefaultScheduler<Guid> sut;

        public DefaultSchedulerTests()
        {
            this.schedulerMetadata.Setup(s => s.HeartbeatInterval).Returns(TimeSpan.FromMilliseconds(100));
            this.sut = new DefaultScheduler<Guid>(
                this.schedulerId,
                this.schedulerMetadata.Object,
                this.schedulerMetadataStore.Object,
                this.schedulerStateMonitor.Object,
                this.jobProcessor.Object,
                this.logger.Object);
        }

        [Fact]
        public async Task ShouldStartStopOnlyOnce()
        {
            // Act
            await this.sut.Start();
            await Task.Delay(1000);
            await this.sut.Stop();

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.sut.Start());
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.sut.Stop());
            this.schedulerMetadataStore.Verify(s => s.Heartbeat(this.schedulerId), Times.AtLeast(5));
        }

        [Fact]
        public async Task ShouldStartImmediately()
        {
            // Arrange
            var heartbeatCompleted = false;
            this.schedulerMetadataStore.Setup(s => s.Heartbeat(this.schedulerId)).Returns(async () =>
            {
                await Task.Delay(500);
                heartbeatCompleted = true;
            });

            // Act
            var sw = Stopwatch.StartNew();
            await this.sut.Start();
            var startTime = sw.Elapsed;
            await WaitHelper.WaitForAction(() => heartbeatCompleted);
            sw.Stop();

            // Assert
            Assert.True(startTime.TotalMilliseconds < 100);
            Assert.True(sw.ElapsedMilliseconds >= 500);
        }

        [Fact]
        public async Task VerifyOrderCall()
        {
            // Arrange
            var order = 0;
            this.schedulerMetadataStore.Setup(s => s.AddScheduler(this.schedulerId, this.schedulerMetadata.Object))
                .Returns(() =>
                {
                    Assert.Equal(1, ++order);
                    return Task.CompletedTask;
                });

            this.schedulerMetadataStore.Setup(s => s.Heartbeat(this.schedulerId)).Returns(() =>
            {
                Assert.Equal(2, ++order);
                return Task.CompletedTask;
            });

            this.schedulerStateMonitor.Setup(s => s.MonitorClusterState(this.schedulerId, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Assert.Equal(3, ++order);
                    return Task.CompletedTask;
                });

            this.jobProcessor.Setup(s => s.ProcessJobs(this.schedulerId, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    Assert.Equal(4, ++order);
                    await this.sut.Stop();
                });

            this.schedulerMetadataStore.Setup(s => s.RemoveScheduler(this.schedulerId))
                .Returns(() =>
                {
                    Assert.Equal(5, ++order);
                    return Task.CompletedTask;
                });

            // Act
            await this.sut.Start();
            await WaitHelper.WaitForAction(() => order == 5);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.sut.Stop());
        }

        [Theory]
        [AutoMoqInlineData(typeof(Exception))]
        [AutoMoqInlineData(typeof(OperationCanceledException))]
        public async Task ShouldHandleHeartbeatException(Type exceptionType)
        {
            // Arrange
            var ex = (Exception)Activator.CreateInstance(exceptionType);
            this.schedulerMetadataStore.Setup(s => s.Heartbeat(this.schedulerId)).ThrowsAsync(ex);

            // Act
            await this.sut.Start();
            await Task.Delay(400);

            // Assert
            this.schedulerStateMonitor.Verify(s => s.MonitorClusterState(this.schedulerId, It.IsAny<CancellationToken>()), Times.AtLeast(2));
            this.jobProcessor.Verify(s => s.ProcessJobs(this.schedulerId, It.IsAny<CancellationToken>()), Times.AtLeast(2));
            this.schedulerMetadataStore.VerifyAll();
        }

        [Theory]
        [AutoMoqInlineData(typeof(Exception))]
        [AutoMoqInlineData(typeof(OperationCanceledException))]
        public async Task ShouldHandleClusterMonitorException(Type exceptionType)
        {
            // Arrange
            var ex = (Exception)Activator.CreateInstance(exceptionType);
            this.schedulerStateMonitor.Setup(s => s.MonitorClusterState(this.schedulerId, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act
            await this.sut.Start();
            await Task.Delay(400);

            // Assert
            this.schedulerMetadataStore.Verify(s => s.Heartbeat(this.schedulerId), Times.AtLeast(2));
            this.jobProcessor.Verify(s => s.ProcessJobs(this.schedulerId, It.IsAny<CancellationToken>()), Times.AtLeast(2));
            this.schedulerStateMonitor.VerifyAll();
        }

        [Theory]
        [AutoMoqInlineData(typeof(OperationCanceledException))]
        [AutoMoqInlineData(typeof(Exception))]
        public async Task ShouldHandleJobProcessingException(Type exceptionType)
        {
            // Arrange
            var ex = (Exception)Activator.CreateInstance(exceptionType);
            this.jobProcessor.Setup(s => s.ProcessJobs(this.schedulerId, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act
            await this.sut.Start();
            await Task.Delay(400);

            // Assert
            this.schedulerMetadataStore.Verify(s => s.Heartbeat(this.schedulerId), Times.AtLeast(2));
            this.schedulerStateMonitor.Verify(s => s.MonitorClusterState(this.schedulerId, It.IsAny<CancellationToken>()), Times.AtLeast(2));
            this.jobProcessor.VerifyAll();
        }

        [Fact]
        public async Task ShouldHandleSchedulerStop()
        {
            // Arrange
            var jobProcessorCalled = false;
            var schedulerStopped = false;
            var heartbeatCalledOnStoppedScheduler = false;

            this.jobProcessor.Setup(s => s.ProcessJobs(this.schedulerId, It.IsAny<CancellationToken>()))
            .Returns(async (Guid _, CancellationToken ct) =>
            {
                jobProcessorCalled = true;
                await Task.Delay(1000, ct);
            });

            this.schedulerMetadataStore.Setup(s => s.Heartbeat(this.schedulerId)).Returns(() =>
            {
                if (schedulerStopped)
                {
                    heartbeatCalledOnStoppedScheduler = true;
                }

                return Task.CompletedTask;
            });

            // Act
            await this.sut.Start();
            await WaitHelper.WaitForAction(() => jobProcessorCalled);
            await this.sut.Stop();
            schedulerStopped = true;

            // Assert
            await Task.Delay(200);
            Assert.False(heartbeatCalledOnStoppedScheduler);
        }
    }
}
