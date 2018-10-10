namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Collections.Generic;
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
            (string, Mock<ISchedulerMetadata>)[] failedSchedulers,
            (Guid, IJobMetadata)[] failedJobs,
            (Guid, IJobMetadata)[] timeoutedJobs)
        {
            // Arrange
            this.SetupSchedulersMocks(failed: failedSchedulers);

            this.jobStore.Setup(s => s.GetExecutingJobs(failedSchedulers[0].Item1)).ReturnsAsync(failedJobs);
            this.jobStore.Setup(s => s.GetTimeoutedJobs()).ReturnsAsync(timeoutedJobs);

            // Act
            await this.sut.MonitorClusterState(schedulerId, CancellationToken.None);

            // Assert
            Assert.All(failedSchedulers, f => this.schedulerMetadataStore.Verify(s => s.RemoveScheduler(f.Item1), Times.Once));
            Assert.All(failedJobs, j => this.jobStore.Verify(s => s.FinalizeJob(j.Item1, j.Item2, JobExecutionResult.SchedulerStalled), Times.Once));
            Assert.All(timeoutedJobs, j => this.jobStore.Verify(s => s.FinalizeJob(j.Item1, j.Item2, JobExecutionResult.Timeouted), Times.Once));
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
            this.SetupSchedulersMocks(failed: new[] { failedScheduler });
            this.schedulerMetadataStore.Setup(s => s.RemoveScheduler(failedScheduler.Item1)).ThrowsAsync(ex);
            this.jobStore.Setup(s => s.GetExecutingJobs(failedScheduler.Item1)).ReturnsAsync(new[] { job });

            // Act
            await this.sut.MonitorClusterState(schedulerId, CancellationToken.None);

            // Assert
            this.schedulerMetadataStore.VerifyAll();
            this.jobStore.Verify(s => s.FinalizeJob(job.Item1, job.Item2, JobExecutionResult.SchedulerStalled), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldNotProcessSelfOrActiveScheduler(
            (string, Mock<ISchedulerMetadata>) self,
            (string, Mock<ISchedulerMetadata>)[] failed,
            (string, Mock<ISchedulerMetadata>)[] active)
        {
            // Arrange
            this.SetupSchedulersMocks(active: active, failed: failed.Concat(new[] { self }).ToArray());

            // Act
            await this.sut.MonitorClusterState(self.Item1, CancellationToken.None);

            // Assert
            Assert.All(failed, s => this.schedulerMetadataStore.Verify(m => m.RemoveScheduler(s.Item1), Times.Once));
            Assert.All(failed, s => this.jobStore.Verify(j => j.GetExecutingJobs(s.Item1), Times.Once));
            Assert.All(active.Concat(new[] { self }), a => this.schedulerMetadataStore.Verify(m => m.RemoveScheduler(a.Item1), Times.Never));
            Assert.All(active.Concat(new[] { self }), a => this.jobStore.Verify(j => j.GetExecutingJobs(a.Item1), Times.Never));
        }

        private void SetupSchedulersMocks((string, Mock<ISchedulerMetadata>)[] failed = null, (string, Mock<ISchedulerMetadata>)[] active = null)
        {
            if (failed != null)
            {
                foreach (var sched in failed)
                {
                    sched.Item2.Setup(s => s.HeartbeatTimeout).Returns(TimeSpan.FromSeconds(1));
                    sched.Item2.Setup(s => s.LastCheckin).Returns(DateTime.Now.Date);
                }
            }

            if (active != null)
            {
                foreach (var sched in active)
                {
                    sched.Item2.Setup(s => s.HeartbeatTimeout).Returns(TimeSpan.FromDays(10));
                    sched.Item2.Setup(s => s.LastCheckin).Returns(DateTime.Now.Date);
                }
            }

            IEnumerable<(string, ISchedulerMetadata)> allSchedulers = Array.Empty<(string, ISchedulerMetadata)>();
            if (failed != null)
            {
                allSchedulers = allSchedulers.Concat(failed.Select(f => (f.Item1, f.Item2.Object)));
            }

            if (active != null)
            {
                allSchedulers = allSchedulers.Concat(active.Select(f => (f.Item1, f.Item2.Object)));
            }

            this.schedulerMetadataStore.Setup(s => s.GetSchedulers()).ReturnsAsync(allSchedulers.ToArray());
        }
    }
}
