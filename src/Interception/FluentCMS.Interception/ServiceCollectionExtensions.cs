namespace FluentCMS.Interception;

public static class ServiceCollectionExtensions
{
    public static AspectInterceptorBuilder<TService, TImplementation> AddInterceptedTransient<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return new AspectInterceptorBuilder<TService, TImplementation>(services, ServiceLifetime.Transient);
    }

    public static AspectInterceptorBuilder<TService, TImplementation> AddInterceptedScoped<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return new AspectInterceptorBuilder<TService, TImplementation>(services, ServiceLifetime.Scoped);
    }

    public static AspectInterceptorBuilder<TService, TImplementation> AddInterceptedSingleton<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return new AspectInterceptorBuilder<TService, TImplementation>(services, ServiceLifetime.Singleton);
    }
}