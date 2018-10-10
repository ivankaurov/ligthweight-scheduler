namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Core;
    using Lightweight.Scheduler.Tests.Unit.Utils;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class DefaultClusterStateMonitorTests
    {
        private readonly Mock<ISchedulerMetadataStore<string>> schedulerMetadataStore = new Mock<ISchedulerMetadataStore<string>>();
        private readonly Mock<IJobStore<Guid, string>> jobStore = new Mock<IJobStore<Guid, string>>();
        private readonly Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>();
        private readonly Mock<ILogger<DefaultClusterStateMonitor<string, Guid>>> logger = new Mock<ILogger<DefaultClusterStateMonitor<string, Guid>>>();
        private readonly DefaultClusterStateMonitor<string, Guid> sut;

        public DefaultClusterStateMonitorTests()
        {
            this.dateTimeProvider.Setup(s => s.Now()).Returns(DateTimeOffset.UtcNow);

            this.sut = new DefaultClusterStateMonitor<string, Guid>(
                this.schedulerMetadataStore.Object,
                this.jobStore.Object,
                this.dateTimeProvider.Object,
                this.logger.Object);
        }

        [Theory]
        [AutoMoqData]
        public async Task VerifyCallOrder(
            string schedulerId,
            string failedSchedulerId,
            Mock<ISchedulerMetadata> failedScheduler,
            (Guid, IJobMetadata)[] failedJobs,
            (Guid, IJobMetadata)[] timeoutedJobs)
        {
            // Arrange
            this.schedulerMetadataStore.Setup(s => s.GetSchedulers()).ReturnsAsync(new[] { (failedSchedulerId, failedScheduler.Object) });

            failedScheduler.Setup(s => s.HeartbeatTimeout).Returns(TimeSpan.FromSeconds(1));
            failedScheduler.Setup(s => s.LastCheckin).Returns(DateTime.Now.Date);

            this.jobStore.Setup(s => s.GetExecutingJobs(failedSchedulerId)).ReturnsAsync(failedJobs);
            this.jobStore.Setup(s => s.GetTimeoutedJobs()).ReturnsAsync(timeoutedJobs);

            // Act
            await this.sut.MonitorClusterState(schedulerId, CancellationToken.None);

            // Assert
            this.schedulerMetadataStore.Verify(s => s.RemoveScheduler(failedSchedulerId), Times.Once);
            Assert.All(failedJobs, j => this.jobStore.Verify(s => s.FinalizeJob(j.Item1, j.Item2, JobExecutionResult.SchedulerStalled), Times.Once));
            Assert.All(timeoutedJobs, j => this.jobStore.Verify(s => s.FinalizeJob(j.Item1, j.Item2, JobExecutionResult.Timeouted), Times.Once));
        }
    }
}
