namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
            this.job.Setup(s => s.Invoke(It.IsAny<IJobMetadata>(), It.IsAny<CancellationToken>()))
                .Returns<IJobMetadata, CancellationToken>((_, ct) =>
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
            Guid jobId,
            IJobMetadata jobMetadata,
            string schedluerId,
            CancellationToken cancellationToken)
        {
            // Arrange
            this.syncHelper.Setup(s => s.WaitOne(cancellationToken)).ReturnsAsync(false);

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata, schedluerId, cancellationToken);

            // Assert
            Assert.Equal(JobExecutionResult.NotStarted, result);
            this.syncHelper.Verify(s => s.Release(), Times.Never);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldReturnFalseWhenJobIsCapturedBySomeonElse(
            Guid jobId,
            IJobMetadata jobMetadata,
            string schedluerId,
            CancellationToken cancellationToken)
        {
            // Arrange
            this.jobStore.Setup(s => s.SetJobOwner(jobId, schedluerId)).ThrowsAsync(new ConcurrencyException());

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata, schedluerId, cancellationToken);

            // Assert
            Assert.Equal(JobExecutionResult.NotStarted, result);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldRethrowExceptionOnCaptureJobFailue(
            Exception ex,
            Guid jobId,
            IJobMetadata jobMetadata,
            string schedluerId,
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
        [AutoMoqInlineData(null)]
        [AutoMoqInlineData(0)]
        [AutoMoqInlineData(-1)]
        [AutoMoqInlineData(100)]
        public async Task ShouldUseParentCancellationToken(
           int? localTimeout,
           Guid jobId,
           string schedluerId,
           Mock<IJobMetadata> jobMetadata,
           CancellationTokenSource cancellationTokenSource)
        {
            // Arrange
            jobMetadata.Setup(s => s.Timeout).Returns(localTimeout == null ? null : new TimeSpan?(TimeSpan.FromSeconds(localTimeout.Value)));
            this.jobFactory.Setup(s => s.CreateJobInstance(jobMetadata.Object)).Returns<IJobMetadata>(_ =>
            {
                cancellationTokenSource.Cancel();
                return this.job.Object;
            });

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata.Object, schedluerId, cancellationTokenSource.Token);

            // Assert
            Assert.Equal(JobExecutionResult.Cancelled, result);
            this.jobStore.Verify(s => s.FinalizeJob(jobId, jobMetadata.Object, JobExecutionResult.Cancelled), Times.Once);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
            jobMetadata.Verify(s => s.SetNextExecutionTime(), Times.Never);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldObserveLocalTimeoutAndRecheduleAsUsual(
           Guid jobId,
           string schedluerId,
           Mock<IJobMetadata> jobMetadata)
        {
            // Arrange
            jobMetadata.Setup(s => s.Timeout).Returns(TimeSpan.FromMilliseconds(100));
            this.job.Setup(j => j.Invoke(jobMetadata.Object, It.IsAny<CancellationToken>()))
                .Returns<IJobMetadata, CancellationToken>(async (_, token) =>
                {
                    await Task.Delay(1000, token);
                });

            // Act
            var sw = Stopwatch.StartNew();
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata.Object, schedluerId, CancellationToken.None);
            sw.Stop();

            // Assert
            Assert.Equal(JobExecutionResult.Timeouted, result);
            Assert.True(sw.Elapsed.TotalMilliseconds < 500);
            this.jobStore.Verify(s => s.FinalizeJob(jobId, jobMetadata.Object, JobExecutionResult.Timeouted), Times.Once);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
            jobMetadata.Verify(s => s.SetNextExecutionTime(), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldRescheduleFailedJobAsUsual(
          Guid jobId,
          string schedluerId,
          Mock<IJobMetadata> jobMetadata,
          Exception ex)
        {
            // Arrange
            this.job.Setup(j => j.Invoke(jobMetadata.Object, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata.Object, schedluerId, CancellationToken.None);

            // Assert
            Assert.Equal(JobExecutionResult.Failed, result);
            this.jobStore.Verify(s => s.FinalizeJob(jobId, jobMetadata.Object, JobExecutionResult.Failed), Times.Once);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
            jobMetadata.Verify(s => s.SetNextExecutionTime(), Times.Once);
        }

        [Theory]
        [AutoMoqInlineData(typeof(ConcurrencyException))]
        [AutoMoqInlineData(typeof(Exception))]
        public async Task ShouldExecuteSimpleJobAndSwallowFinalizeJobFailures(
          Type clearJobOwnerFailureException,
          Guid jobId,
          string schedluerId,
          Mock<IJobMetadata> jobMetadata)
        {
            // Arrange
            if (clearJobOwnerFailureException != null)
            {
                var ex = (Exception)Activator.CreateInstance(clearJobOwnerFailureException);
                this.jobStore.Setup(s => s.FinalizeJob(jobId, jobMetadata.Object, It.IsAny<JobExecutionResult>())).ThrowsAsync(ex);
            }

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata.Object, schedluerId, CancellationToken.None);

            // Assert
            Assert.Equal(JobExecutionResult.Succeeded, result);
            this.jobStore.Verify(s => s.FinalizeJob(jobId, jobMetadata.Object, JobExecutionResult.Succeeded), Times.Once);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
            jobMetadata.Verify(s => s.SetNextExecutionTime(), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldExecuteSimpleJobAndSwallowCalculateNextTimeException(
         Exception ex,
         Guid jobId,
         string schedluerId,
         Mock<IJobMetadata> jobMetadata)
        {
            // Arrange
            jobMetadata.Setup(s => s.SetNextExecutionTime()).Throws(ex);

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata.Object, schedluerId, CancellationToken.None);

            // Assert
            Assert.Equal(JobExecutionResult.Succeeded, result);
            this.jobStore.Verify(s => s.FinalizeJob(jobId, jobMetadata.Object, JobExecutionResult.Succeeded), Times.Once);
            this.syncHelper.Verify(s => s.Release(), Times.Once);
            jobMetadata.Verify(s => s.SetNextExecutionTime(), Times.Once);
        }

        [Theory]
        [AutoMoqInlineData(-1)]
        [AutoMoqInlineData(10000)]
        public async Task ShouldUseCorrectCallOrder(
            int jobTimeout,
            Guid jobId,
            string schedulerId,
            Mock<IJobMetadata> jobMetadata,
            Mock<IDisposable> scope)
        {
            // Arrange
            jobMetadata.Setup(s => s.Timeout).Returns(TimeSpan.FromMilliseconds(jobTimeout));
            var cancellationTokenSource = new CancellationTokenSource();
            var order = 0;
            this.syncHelper.Setup(s => s.WaitOne(cancellationTokenSource.Token)).Returns(() =>
            {
                Assert.Equal(1, ++order);
                return Task.FromResult(true);
            });

            this.jobStore.Setup(s => s.SetJobOwner(jobId, schedulerId)).Returns(() =>
            {
                Assert.Equal(2, ++order);
                return Task.CompletedTask;
            });

            this.jobFactory.Setup(s => s.BeginScope()).Returns(() =>
            {
                Assert.Equal(3, ++order);
                return scope.Object;
            });

            this.jobFactory.Setup(s => s.CreateJobInstance(jobMetadata.Object)).Returns(() =>
            {
                Assert.Equal(4, ++order);
                return this.job.Object;
            });

            this.job.Setup(s => s.Invoke(jobMetadata.Object, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Assert.Equal(5, ++order);
                    return Task.CompletedTask;
                });

            scope.Setup(s => s.Dispose()).Callback(() =>
            {
                Assert.Equal(6, ++order);
            });

            jobMetadata.Setup(s => s.SetNextExecutionTime()).Callback(() => Assert.Equal(7, ++order));

            this.jobStore.Setup(s => s.FinalizeJob(jobId, jobMetadata.Object, JobExecutionResult.Succeeded)).Returns(() =>
            {
                Assert.Equal(8, ++order);
                return Task.CompletedTask;
            });

            this.syncHelper.Setup(s => s.Release()).Callback(() => Assert.Equal(9, ++order));

            // Act
            var result = await this.sut.ProcessSingleJob(jobId, jobMetadata.Object, schedulerId, cancellationTokenSource.Token);

            // Assert
            Assert.Equal(9, order);
        }
    }
}
