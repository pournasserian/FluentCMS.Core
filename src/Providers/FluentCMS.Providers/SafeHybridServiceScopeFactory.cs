using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers;

// Service scope factory that can resolve from both the plugin and parent providers with safety checks
public class SafeHybridServiceScopeFactory(
    IServiceScopeFactory localFactory,
    IRootServiceProvider rootServiceProvider,
    AssemblyContext context) : IServiceScopeFactory
{
    private readonly IServiceScopeFactory _rootFactory = rootServiceProvider.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

    public IServiceScope CreateScope()
    {
        context.ThrowIfDisposed();
        return new SafeHybridServiceScope(context.CreateScope(), _rootFactory.CreateScope(), context);
    }

    private class SafeHybridServiceScope(IServiceScope localScope, IServiceScope rootScope, AssemblyContext context) : IServiceScope
    {
        private bool _disposed;

        public IServiceProvider ServiceProvider { get; } = new SafeHybridServiceProvider(localScope.ServiceProvider, rootScope.ServiceProvider, context);

        public void Dispose()
        {
            if (_disposed) return;

            localScope.Dispose();
            rootScope.Dispose();
            _disposed = true;
        }
    }
}