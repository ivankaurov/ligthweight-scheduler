namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Core;
    using Lightweight.Scheduler.Tests.Unit.Utils;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Xunit;

    public class DefaultJobFactoryTests
    {
        [Theory]
        [AutoMoqInlineData(ServiceLifetime.Scoped, 2)]
        [AutoMoqInlineData(ServiceLifetime.Singleton, 1)]
        [AutoMoqInlineData(ServiceLifetime.Transient, 3)]
        public void ShouldCreateJobInstanceInScope(ServiceLifetime serviceLifetime, int disctictInstanceCount, Mock<IJobMetadata> jobMetadata)
        {
            // Arrange
            jobMetadata.Setup(s => s.JobClass).Returns(typeof(TestJob));
            var sut = new DefaultJobFactory(this.CreateServiceProvider(jobMetadata.Object, serviceLifetime));

            // Act
            var results = new List<IJob>(3);
            using (sut.BeginScope())
            {
                results.Add(sut.CreateJobInstance(jobMetadata.Object));
                using (sut.BeginScope())
                {
                    results.Add(sut.CreateJobInstance(jobMetadata.Object));
                }

                results.Add(sut.CreateJobInstance(jobMetadata.Object));
            }

            // Assert
            Assert.All(results, job => Assert.IsType<TestJob>(job));
            Assert.Equal(disctictInstanceCount, results.Cast<TestJob>().Select(j => j.JobId).Distinct().Count());
        }

        [Theory]
        [AutoMoqInlineData(ServiceLifetime.Singleton, 1)]
        [AutoMoqInlineData(ServiceLifetime.Transient, 2)]
        public void ShouldCreateJobInstancesWithoutScope(ServiceLifetime serviceLifetime, int distictJobInstance, Mock<IJobMetadata> jobMetadata)
        {
            // Arrange
            jobMetadata.Setup(s => s.JobClass).Returns(typeof(TestJob));
            var sut = new DefaultJobFactory(this.CreateServiceProvider(jobMetadata.Object, serviceLifetime));

            // Act
            var results = new List<TestJob>(2);
            results.Add((TestJob)sut.CreateJobInstance(jobMetadata.Object));
            results.Add((TestJob)sut.CreateJobInstance(jobMetadata.Object));

            // Assert
            Assert.Equal(distictJobInstance, results.Select(j => j.JobId).Distinct().Count());
        }

        [Theory]
        [AutoMoqData]
        public async Task ShouldDisposeChildScopesWhenParentDisposed(Mock<IJobMetadata> jobMetadata)
        {
            // Arrange
            jobMetadata.Setup(s => s.JobClass).Returns(typeof(TestJob));
            var sut = new DefaultJobFactory(this.CreateServiceProvider(jobMetadata.Object, ServiceLifetime.Scoped));

            // Act
            IJob parentJob;
            IJob childJob;
            using (sut.BeginScope())
            {
                var rootJob = sut.CreateJobInstance(jobMetadata.Object);

                using (sut.BeginScope())
                {
                    parentJob = sut.CreateJobInstance(jobMetadata.Object);

                    sut.BeginScope();
                    childJob = sut.CreateJobInstance(jobMetadata.Object);
                }

                // Assert
                await Assert.ThrowsAsync<ObjectDisposedException>(() => childJob.Invoke(null, CancellationToken.None));
                await Assert.ThrowsAsync<ObjectDisposedException>(() => parentJob.Invoke(null, CancellationToken.None));
                await rootJob.Invoke(null, CancellationToken.None);
            }
        }

        private IServiceProvider CreateServiceProvider(IJobMetadata jobMetadata, ServiceLifetime serviceLifetime)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Insert(0, new ServiceDescriptor(jobMetadata.JobClass, jobMetadata.JobClass, serviceLifetime));

            return serviceCollection.BuildServiceProvider();
        }

        private sealed class TestJob : IJob, IDisposable
        {
            private bool objectDisposed;

            public Guid JobId { get; } = Guid.NewGuid();

            public void Dispose()
            {
                this.objectDisposed = true;
            }

            public Task Invoke(IJobMetadata jobMetadata, CancellationToken cancellationToken)
            {
                if (this.objectDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return Task.CompletedTask;
            }
        }
    }
}
