using System.Reflection;

namespace FluentCMS.Core.Interception;

public class InterceptorGroup<TService, TImplementation> where TService : class where TImplementation : class, TService
{
    private readonly AspectInterceptorBuilder<TService, TImplementation> _builder;
    private readonly InterceptorRegistration _registration;

    public InterceptorGroup(AspectInterceptorBuilder<TService, TImplementation> builder, InterceptorRegistration registration)
    {
        _builder = builder;
        _registration = registration;
    }

    public InterceptorGroup<TService, TImplementation> AddInterceptor(IAspectInterceptor interceptor)
    {
        _registration.AddInterceptor(interceptor);
        return this;
    }

    public InterceptorGroup<TService, TImplementation> WithMethodFilter(Func<MethodInfo, bool> filter)
    {
        _registration.WithMethodFilter(filter);
        return this;
    }

    public AspectInterceptorBuilder<TService, TImplementation> EndGroup()
    {
        return _builder;
    }
}
