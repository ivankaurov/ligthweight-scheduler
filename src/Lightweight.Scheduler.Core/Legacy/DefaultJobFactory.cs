namespace Lightweight.Scheduler.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Lightweight.Scheduler.Abstractions;
    using Microsoft.Extensions.DependencyInjection;

    internal sealed class DefaultJobFactory : IJobFactory
    {
        private readonly IServiceProvider serviceProvider;

        private readonly AsyncLocal<Stack<ServiceScopeWrapper>> scopes = new AsyncLocal<Stack<ServiceScopeWrapper>>();

        public DefaultJobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IDisposable BeginScope()
        {
            IServiceProvider provider;
            if (this.scopes.Value == null)
            {
                this.scopes.Value = new Stack<ServiceScopeWrapper>();
                provider = this.serviceProvider;
            }
            else
            {
                provider = this.scopes.Value.Count > 0 ? this.scopes.Value.Peek().Scope.ServiceProvider : this.serviceProvider;
            }

            var scope = new ServiceScopeWrapper(provider.CreateScope(), this);
            try
            {
                this.scopes.Value.Push(scope);
                return scope;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
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
                throw new ArgumentException($"{jobMetadata.JobClass} doesn't implement {nameof(IJob)}", nameof(jobMetadata));
            }

            var provider = this.scopes.Value?.Count > 0 ? this.scopes.Value.Peek().Scope.ServiceProvider : this.serviceProvider;
            return (IJob)provider.GetRequiredService(jobMetadata.JobClass);
        }

        private sealed class ServiceScopeWrapper : IDisposable
        {
            private readonly DefaultJobFactory owner;
            private bool objectDisposed;

            public ServiceScopeWrapper(IServiceScope scope, DefaultJobFactory owner)
            {
                this.Scope = scope;
                this.owner = owner;
            }

            public IServiceScope Scope { get; }

            public void Dispose()
            {
                if (!this.objectDisposed)
                {
                    ServiceScopeWrapper childScope;
                    while (this.owner.scopes.Value?.Count > 0 && !ReferenceEquals(this, childScope = this.owner.scopes.Value.Pop()))
                    {
                        childScope.CloseScope();
                    }

                    this.CloseScope();
                }
            }

            private void CloseScope()
            {
                if (!this.objectDisposed)
                {
                    this.Scope.Dispose();
                    this.objectDisposed = true;
                }
            }
        }
    }
}
