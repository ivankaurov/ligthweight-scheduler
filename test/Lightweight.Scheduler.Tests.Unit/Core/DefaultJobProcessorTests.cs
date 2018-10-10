namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Core;
    using Lightweight.Scheduler.Tests.Unit.Utils;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class DefaultJobProcessorTests
    {
        private readonly Mock<ISingleJobProcessor<string, Guid>> singleJobProcessor = new Mock<ISingleJobProcessor<string, Guid>>();
        private readonly Mock<IJobStore<Guid, string>> jobStore = new Mock<IJobStore<Guid, string>>();
        private readonly Mock<ILogger<DefaultJobProcessor<string, Guid>>> logger = new Mock<ILogger<DefaultJobProcessor<string, Guid>>>();
        private readonly DefaultJobProcessor<string, Guid> sut;

        public DefaultJobProcessorTests()
        {
            this.sut = new DefaultJobProcessor<string, Guid>(
                this.singleJobProcessor.Object,
                this.jobStore.Object,
                this.logger.Object);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldReturnImmediatelyOnLongJobsProcessingAndExecuteAllJobs((Guid id, IJobMetadata metadata)[] jobs, string schedulerId)
        {
            // Arrange
            var jobsExecuted = 0;
            this.jobStore.Setup(s => s.GetJobsForExecution()).ReturnsAsync(jobs);
            this.singleJobProcessor.Setup(s => s.ProcessSingleJob(
                It.IsAny<Guid>(),
                It.IsAny<IJobMetadata>(),
                schedulerId,
                CancellationToken.None)).Returns(() =>
                {
                    Thread.Sleep(1000);
                    Interlocked.Increment(ref jobsExecuted);
                    return Task.FromResult(JobExecutionResult.Succeeded);
                });

            // Act
            var sw = Stopwatch.StartNew();
            await this.sut.ProcessJobs(schedulerId, CancellationToken.None);
            var callTime = sw.Elapsed;
            await WaitHelper.WaitForAction(() => jobsExecuted == jobs.Length);
            sw.Stop();
            var executionTime = sw.Elapsed;

            // Assert
            Assert.True(callTime.TotalMilliseconds < 500);
            Assert.True(executionTime.TotalMilliseconds >= 1000);
            Assert.True(executionTime.TotalMilliseconds < 999 * jobs.Length);
            Assert.All(jobs, j => this.singleJobProcessor.Verify(s => s.ProcessSingleJob(j.id, j.metadata, schedulerId, CancellationToken.None), Times.Once));
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldHandleOperationCanceledException((Guid id, IJobMetadata metadata) job, string schedulerId)
        {
            // Arrange
            var exceptionThrown = false;
            var cts = new CancellationTokenSource(300);
            this.jobStore.Setup(s => s.GetJobsForExecution()).ReturnsAsync(new[] { job });
            this.singleJobProcessor.Setup(s => s.ProcessSingleJob(
                job.id,
                job.metadata,
                schedulerId,
                cts.Token)).Returns(async (Guid jobId, IJobMetadata metadata, string sched, CancellationToken token) =>
                {
                    try
                    {
                        await Task.Delay(1000, token);
                        return JobExecutionResult.Succeeded;
                    }
                    catch (OperationCanceledException)
                    {
                        exceptionThrown = true;
                        throw;
                    }
                });

            // Act
            var sw = Stopwatch.StartNew();
            await this.sut.ProcessJobs(schedulerId, cts.Token);
            var callTime = sw.Elapsed;
            await WaitHelper.WaitForAction(() => exceptionThrown);
            sw.Stop();
            var executionTime = sw.Elapsed;

            // Assert
            Assert.True(callTime.TotalMilliseconds < 100);
            Assert.True(executionTime.TotalMilliseconds >= 200);
            Assert.True(executionTime.TotalMilliseconds < 400);
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldHandleJobExecutionException(Exception ex, (Guid id, IJobMetadata metadata) job, string schedulerId)
        {
            // Arrange
            var exceptionThrown = false;
            this.jobStore.Setup(s => s.GetJobsForExecution()).ReturnsAsync(new[] { job });
            this.singleJobProcessor.Setup(s => s.ProcessSingleJob(
                job.id,
                job.metadata,
                schedulerId,
                CancellationToken.None)).Returns(async () =>
                {
                    await Task.Delay(100);
                    exceptionThrown = true;
                    throw ex;
                });

            // Act
            var sw = Stopwatch.StartNew();
            await this.sut.ProcessJobs(schedulerId, CancellationToken.None);
            var callTime = sw.Elapsed;
            await WaitHelper.WaitForAction(() => exceptionThrown);
            sw.Stop();
            var executionTime = sw.Elapsed;

            // Assert
            Assert.True(callTime.TotalMilliseconds < 100);
            Assert.True(executionTime.TotalMilliseconds >= 100);
            Assert.True(executionTime.TotalMilliseconds < 200);
        }
    }
}
