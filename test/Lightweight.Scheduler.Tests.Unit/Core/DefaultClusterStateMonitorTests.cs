namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Abstractions.Exceptions;
    using Lightweight.Scheduler.Core;
    using Lightweight.Scheduler.Tests.Unit.Utils;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class DefaultClusterStateMonitorTests
    {
        private readonly Mock<ISchedulerMetadataStore<string>> schedulerMetadataStore = new Mock<ISchedulerMetadataStore<string>>();
        private readonly Mock<IJobStore<Guid, string>> jobStore = new Mock<IJobStore<Guid, string>>();
        private readonly Mock<ILogger<DefaultClusterStateMonitor<string, Guid>>> logger = new Mock<ILogger<DefaultClusterStateMonitor<string, Guid>>>();
        private readonly DefaultClusterStateMonitor<string, Guid> sut;

        public DefaultClusterStateMonitorTests()
        {
            this.SetupSchedulersMocks();
            this.jobStore.Setup(s => s.GetTimeoutedJobs()).ReturnsAsync(Array.Empty<(Guid, IJobMetadata)>());

            this.sut = new DefaultClusterStateMonitor<string, Guid>(
                this.schedulerMetadataStore.Object,
                this.jobStore.Object,
                this.logger.Object);
        }

        [Theory]
        [AutoMoqData]
        public async Task VerifyCallOrder(
            string schedulerId,
            (string, Mock<ISchedulerMetadata>)[] stalledSchedulers,
            (Guid, Mock<IJobMetadata>)[] failedJobs,
            (Guid, Mock<IJobMetadata>)[] timeoutedJobs)
        {
            // Arrange
            this.SetupSchedulersMocks(stalledSchedulers);
            this.SetupJobStoreMocks(stalledSchedulers[0].Item1, failedJobs, timeoutedJobs);

            // Act
            await this.sut.MonitorClusterState(schedulerId, CancellationToken.None);

            // Assert
            Assert.All(stalledSchedulers, f => this.schedulerMetadataStore.Verify(s => s.RemoveScheduler(f.Item1), Times.Once));
            Assert.All(failedJobs, j => this.jobStore.Verify(s => s.RecoverJob(j.Item1, JobExecutionResult.SchedulerStalled), Times.Once));
            Assert.All(failedJobs, j => j.Item2.Verify(s => s.SetNextExecutionTime(), Times.Never));
            Assert.All(timeoutedJobs, j => this.jobStore.Verify(s => s.FinalizeJob(j.Item1, j.Item2.Object, JobExecutionResult.Timeouted), Times.Once));
            Assert.All(timeoutedJobs, j => j.Item2.Verify(s => s.SetNextExecutionTime(), Times.Once));
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldProcessJobsOnStalledSchedulerProcessedBySomeoneElse(
            string schedulerId,
            (string, Mock<ISchedulerMetadata>) failedScheduler,
            (Guid, IJobMetadata) job,
            ConcurrencyException ex)
        {
            // Arrange
            this.SetupSchedulersMocks(new[] { failedScheduler });
            this.schedulerMetadataStore.Setup(s => s.RemoveScheduler(failedScheduler.Item1)).ThrowsAsync(ex);
            this.jobStore.Setup(s => s.GetExecutingJobs(failedScheduler.Item1)).ReturnsAsync(new[] { job });

            // Act
            await this.sut.MonitorClusterState(schedulerId, CancellationToken.None);

            // Assert
            this.schedulerMetadataStore.VerifyAll();
            this.jobStore.Verify(s => s.RecoverJob(job.Item1, JobExecutionResult.SchedulerStalled), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldNotProcessSelfOrActiveScheduler(
            (string, Mock<ISchedulerMetadata>) self,
            (string, Mock<ISchedulerMetadata>)[] stalled)
        {
            // Arrange
            this.SetupSchedulersMocks(stalled.Concat(new[] { self }).ToArray());

            // Act
            await this.sut.MonitorClusterState(self.Item1, CancellationToken.None);

            // Assert
            Assert.All(stalled, s => this.schedulerMetadataStore.Verify(m => m.RemoveScheduler(s.Item1), Times.Once));
            Assert.All(stalled, s => this.jobStore.Verify(j => j.GetExecutingJobs(s.Item1), Times.Once));

            this.schedulerMetadataStore.Verify(s => s.RemoveScheduler(self.Item1), Times.Never);
            this.jobStore.Verify(s => s.GetExecutingJobs(self.Item1), Times.Never);
        }

        private void SetupSchedulersMocks((string, Mock<ISchedulerMetadata>)[] stalled = null)
        {
            this.schedulerMetadataStore.Setup(s => s.GetStalledSchedulers())
                .ReturnsAsync(stalled?.Select(s => (s.Item1, s.Item2.Object)).ToArray() ?? Array.Empty<(string, ISchedulerMetadata)>());
        }

        private void SetupJobStoreMocks(string failedSchedulerId = null, (Guid, Mock<IJobMetadata>)[] failed = null, (Guid, Mock<IJobMetadata>)[] timeouted = null)
        {
            if (failed != null)
            {
                this.jobStore.Setup(s => s.GetExecutingJobs(failedSchedulerId)).ReturnsAsync(failed.Select(f => (f.Item1, f.Item2.Object)).ToArray());
            }

            if (timeouted != null)
            {
                this.jobStore.Setup(s => s.GetTimeoutedJobs()).ReturnsAsync(timeouted.Select(j => (j.Item1, j.Item2.Object)).ToArray());
            }
        }
    }
}
