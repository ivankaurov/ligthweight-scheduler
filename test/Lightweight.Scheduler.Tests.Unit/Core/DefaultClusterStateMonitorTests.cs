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
        public async Task VerifyCallOrder(string failedSchedulerId, Mock<ISchedulerMetadata> failedScheduler)
        {
            // Arrange
            var order = 0;
            this.schedulerMetadataStore.Setup(s => s.GetSchedulers()).Returns(async () =>
            {
                await Task.Yield();
                Assert.Equal(1, ++order);
                return new[] { (failedSchedulerId, failedScheduler.Object) };
            });

            await Task.Yield();
        }
    }
}
