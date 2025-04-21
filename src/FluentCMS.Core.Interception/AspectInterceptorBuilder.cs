namespace FluentCMS.Core.Interception;

public class AspectInterceptorBuilder<TService, TImplementation> where TService : class where TImplementation : class, TService
{
    private readonly List<InterceptorRegistration> _interceptorRegistrations = [];
    private readonly IServiceCollection _services;
    private readonly ServiceLifetime _lifetime;

    public AspectInterceptorBuilder(IServiceCollection services, ServiceLifetime lifetime)
    {
        _services = services;
        _lifetime = lifetime;
    }

    public AspectInterceptorBuilder<TService, TImplementation> AddInterceptor(IAspectInterceptor interceptor)
    {
        var registration = new InterceptorRegistration();
        registration.AddInterceptor(interceptor);
        _interceptorRegistrations.Add(registration);
        return this;
    }

    public AspectInterceptorBuilder<TService, TImplementation> AddInterceptor(IAspectInterceptor interceptor, Func<MethodInfo, bool> methodFilter)
    {
        var registration = new InterceptorRegistration();
        registration.AddInterceptor(interceptor);
        registration.WithMethodFilter(methodFilter);
        _interceptorRegistrations.Add(registration);
        return this;
    }

    public InterceptorGroup<TService, TImplementation> CreateInterceptorGroup()
    {
        var registration = new InterceptorRegistration();
        _interceptorRegistrations.Add(registration);
        return new InterceptorGroup<TService, TImplementation>(this, registration);
    }

    public IServiceCollection Build()
    {
        // Register the implementation
        switch (_lifetime)
        {
            case ServiceLifetime.Singleton:
                _services.AddSingleton<TImplementation>();
                break;
            case ServiceLifetime.Scoped:
                _services.AddScoped<TImplementation>();
                break;
            case ServiceLifetime.Transient:
                _services.AddTransient<TImplementation>();
                break;
        }

        // Register the proxy
        var descriptorService = ServiceDescriptor.Describe(typeof(TService), provider =>
        {
            var implementation = provider.GetRequiredService<TImplementation>();
            var proxy = DispatchProxy.Create<TService, AspectProxy>();
            ((AspectProxy)(object)proxy).Initialize(implementation, provider, _interceptorRegistrations);
            return proxy;
        },
        _lifetime);

        _services.Add(descriptorService);

        return _services;
    }
}
