namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Lightweight.Scheduler.Abstractions;
    using Microsoft.Extensions.DependencyInjection;

    internal sealed class DefaultJobFactory : IJobFactory
    {
        private readonly IServiceProvider serviceProvider;

        private readonly AsyncLocal<Stack<IServiceScope>> scopes = new AsyncLocal<Stack<IServiceScope>>();

        public DefaultJobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IDisposable BeginScope()
        {
            IServiceProvider provider;
            if (this.scopes.Value == null)
            {
                this.scopes.Value = new Stack<IServiceScope>();
                provider = this.serviceProvider;
            }
            else
            {
                provider = this.scopes.Value.Count > 0 ? this.scopes.Value.Peek().ServiceProvider : this.serviceProvider;
            }

            var scope = provider.CreateScope();
            this.scopes.Value.Push(scope);
            return new ServiceScopeWrapper(scope, this);
        }

        public IJob CreateJobInstance(IJobMetadata jobMetadata)
        {
            if (jobMetadata == null)
            {
                throw new ArgumentNullException(nameof(jobMetadata));
            }

            if (jobMetadata.JobClass == null)
            {
                throw new ArgumentException(nameof(jobMetadata.JobClass) + " property is null", nameof(jobMetadata));
            }

            if (!typeof(IJob).IsAssignableFrom(jobMetadata.JobClass))
            {
                throw new ArgumentException(nameof(jobMetadata.JobClass) + " doesn't implement " + nameof(IJob), nameof(jobMetadata));
            }

            var provider = this.scopes.Value?.Count > 0 ? this.scopes.Value.Peek().ServiceProvider : this.serviceProvider;
            return (IJob)provider.GetRequiredService(jobMetadata.JobClass);
        }

        private sealed class ServiceScopeWrapper : IDisposable
        {
            private readonly IServiceScope scope;
            private readonly DefaultJobFactory owner;
            private bool objectDisposed;

            public ServiceScopeWrapper(IServiceScope scope, DefaultJobFactory owner)
            {
                this.scope = scope;
                this.owner = owner;
            }

            public void Dispose()
            {
                if (!this.objectDisposed)
                {
                    IServiceScope childScope;
                    while (this.owner.scopes.Value?.Count > 0 && this.scope != (childScope = this.owner.scopes.Value.Pop()))
                    {
                        childScope.Dispose();
                    }

                    this.scope.Dispose();
                    this.objectDisposed = true;
                }
            }
        }
    }
}
