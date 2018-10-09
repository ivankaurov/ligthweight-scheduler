namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Abstractions.Exceptions;
    using Lightweight.Scheduler.Core;
    using Lightweight.Scheduler.Core.Internal;
    using Lightweight.Scheduler.Tests.Unit.Utils;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class DefaultSingleJobProcessorTests
    {
        private const string ExecutedFlag = "JobExecuted";
        private readonly Mock<IJobStore<Guid, string>> jobStore = new Mock<IJobStore<Guid, string>>();
        private readonly Mock<ISyncHelper> syncHelper = new Mock<ISyncHelper>();
        private readonly Mock<IJobFactory> jobFactory = new Mock<IJobFactory>();
        private readonly Mock<IJob> job = new Mock<IJob>();
        private readonly Mock<ILogger<DefaultSingleJobProcessor<string, Guid>>> logger = new Mock<ILogger<DefaultSingleJobProcessor<string, Guid>>>();
        private readonly DefaultSingleJobProcessor<string, Guid> sut;

        public DefaultSingleJobProcessorTests()
        {
            this.job.Setup(s => s.Invoke(It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns<IDictionary<string, object>, CancellationToken>((_, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                });
            this.jobFactory.Setup(s => s.CreateJobInstance(It.IsAny<IJobMetadata>())).Returns(this.job.Object);
            this.syncHelper.Setup(s => s.WaitOne(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            this.sut = new DefaultSingleJobProcessor<string, Guid>(
                this.jobStore.Object,
                this.syncHelper.Object,
                this.jobFactory.Object,
                this.logger.Object);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldReturnFalseOnAllThreadsBusy(
            IIdentifier<Guid> jobId,
            IJobMetadata jobMetadata,
            IIdentifier<string> schedluerId,
            CancellationToken cancellationToken)
        {
            // Arrange
            this.syncHelper.Setup(s => s.WaitOne(cancellationToken)).ReturnsAsync(false);

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata, schedluerId, cancellationToken);

            // Assert
            Assert.False(result);
            this.syncHelper.Verify(s => s.Release(), Times.Never);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldReturnFalseWhenJobIsCapturedBySomeonElse(
            IIdentifier<Guid> jobId,
            IJobMetadata jobMetadata,
            IIdentifier<string> schedluerId,
            CancellationToken cancellationToken)
        {
            // Arrange
            this.jobStore.Setup(s => s.SetJobOwner(jobId, schedluerId)).ThrowsAsync(new ConcurrencyException());

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata, schedluerId, cancellationToken);

            // Assert
            Assert.False(result);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldRethrowExceptionOnCaptureJobFailue(
            Exception ex,
            IIdentifier<Guid> jobId,
            IJobMetadata jobMetadata,
            IIdentifier<string> schedluerId,
            CancellationToken cancellationToken)
        {
            // Arrange
            this.jobStore.Setup(s => s.SetJobOwner(jobId, schedluerId)).ThrowsAsync(ex);

            // Act
            var actualEx = await Assert.ThrowsAsync(ex.GetType(), () => this.sut.ProcessSingleJob(jobId, jobMetadata, schedluerId, cancellationToken));

            // Assert
            Assert.Same(ex, actualEx);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldUseParentCancellationToken(
           IIdentifier<Guid> jobId,
           IIdentifier<string> schedluerId,
           IJobMetadata jobMetadata,
           CancellationTokenSource cancellationTokenSource)
        {
            // Arrange
            this.jobFactory.Setup(s => s.CreateJobInstance(jobMetadata)).Returns<IJobMetadata>(_ =>
            {
                cancellationTokenSource.Cancel();
                return this.job.Object;
            });

            // Act
            await Assert.ThrowsAsync<OperationCanceledException>(() => this.sut.ProcessSingleJob(jobId, jobMetadata, schedluerId, cancellationTokenSource.Token));

            // Assert
            this.jobStore.Verify(s => s.UpdateJob(jobId, jobMetadata, null), Times.Once);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
        }
    }
}
