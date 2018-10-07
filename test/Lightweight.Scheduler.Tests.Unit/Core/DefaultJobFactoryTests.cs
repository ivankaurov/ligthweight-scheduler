namespace Lightweight.Scheduler.Tests.Unit.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lightweight.Scheduler.Abstractions;
    using Lightweight.Scheduler.Core;
    using Lightweight.Scheduler.Tests.Unit.Internal;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Xunit;

    public class DefaultJobFactoryTests
    {
        [Theory]
        [AutoMoqInlineData(ServiceLifetime.Scoped)]
        [AutoMoqInlineData(ServiceLifetime.Singleton)]
        [AutoMoqInlineData(ServiceLifetime.Transient)]
        public void ShouldCreateJobInstanceInScope(ServiceLifetime serviceLifetime, Mock<IJobMetadata> jobMetadata, Mock<IJob> jobMock)
        {
            // Arrange
            jobMetadata.Setup(s => s.JobClass).Returns(jobMock.Object.GetType());
            var sut = new DefaultJobFactory(this.CreateServiceProvider(jobMock.Object.GetType(), serviceLifetime));

            // Act
            IJob result;
            using (sut.BeginScope())
            {
                result = sut.CreateJobInstance(jobMetadata.Object);
            }

            // Assert
            Assert.Equal(jobMock.Object.GetType(), result.GetType());
        }

        private IServiceProvider CreateServiceProvider(Type jobType, ServiceLifetime serviceLifetime)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Insert(0, new ServiceDescriptor(jobType, jobType, serviceLifetime));

            return serviceCollection.BuildServiceProvider();
        }
    }
}
