using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Core.EventBus.FunctionalTests.Infrastructure;

// Custom service provider for fine-grained control over test configurations
public class TestServiceProvider
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _serviceProvider;

    public TestServiceProvider()
    {
        // Add logging by default
        _services.AddLogging();

        // Register the event bus 
        _services.AddEventBus();
    }

    // Register a service
    public TestServiceProvider RegisterService<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddScoped<TService, TImplementation>();
        return this;
    }

    // Register a singleton service
    public TestServiceProvider RegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddSingleton<TService, TImplementation>();
        return this;
    }

    // Register a service instance
    public TestServiceProvider RegisterInstance<TService>(TService instance)
        where TService : class
    {
        _services.AddSingleton(instance);
        return this;
    }

    // Register an event subscriber
    public TestServiceProvider RegisterEventSubscriber<TEvent, TSubscriber>()
        where TSubscriber : class, IEventSubscriber<TEvent>
    {
        _services.AddScoped<TSubscriber>();
        _services.AddSingleton<IEventSubscriber<TEvent>>(sp =>
            sp.GetRequiredService<TSubscriber>());
        return this;
    }

    // Build the service provider
    public IServiceProvider BuildServiceProvider()
    {
        _serviceProvider = _services.BuildServiceProvider();
        return _serviceProvider;
    }

    // Get a service
    public T GetService<T>() where T : class
    {
        if (_serviceProvider == null)
        {
            BuildServiceProvider();
        }

        return _serviceProvider!.GetRequiredService<T>();
    }

    // Dispose the service provider
    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
