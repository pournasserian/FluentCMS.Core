namespace FluentCMS.Providers;

// Service provider that tries local first, then falls back to parent with safety checks
public class SafeHybridServiceProvider(IServiceProvider localProvider, IServiceProvider rootProvider, AssemblyContext context) : IServiceProvider
{
    public object? GetService(Type serviceType)
    {
        context.ThrowIfDisposed();

        // Try to get the service locally first
        // If not found locally, try the root provider
        var service = localProvider.GetService(serviceType) ?? rootProvider.GetService(serviceType);

        return service;
    }
}

