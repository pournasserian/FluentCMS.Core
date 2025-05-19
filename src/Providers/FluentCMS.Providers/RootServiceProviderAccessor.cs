using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers;

// Interface for accessing the root service provider
public interface IRootServiceProvider
{
    IServiceProvider ServiceProvider { get; }
    T GetRootService<T>() where T : class;
}

public class RootServiceProviderAccessor(IServiceProvider serviceProvider) : IRootServiceProvider
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public T GetRootService<T>() where T : class
    {
        return ServiceProvider.GetService<T>() ??
            throw new InvalidOperationException($"Service of type {typeof(T).FullName} not found in the root service provider.");
    }
}