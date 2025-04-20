using System.Reflection;
using FluentCMS.Core.Interception.Abstractions;

namespace FluentCMS.Core.Interception.Framework;

/// <summary>
/// A proxy that intercepts method calls on an instance of type <typeparamref name="TService"/> and routes them through an interceptor chain.
/// </summary>
/// <typeparam name="TService">The type of service to proxy.</typeparam>
public class ServiceProxy<TService> : DispatchProxy where TService : class
{
    private TService? _targetService;
    private IInterceptorChain? _interceptorChain;

    /// <summary>
    /// Creates a proxy for the specified service that intercepts method calls using the provided interceptor chain.
    /// </summary>
    /// <param name="service">The service to proxy.</param>
    /// <param name="interceptorChain">The interceptor chain to use.</param>
    /// <returns>A proxy for the service.</returns>
    public static TService Create(TService service, IInterceptorChain interceptorChain)
    {
        object proxy = Create<TService, ServiceProxy<TService>>();
        (proxy as ServiceProxy<TService>)?.Initialize(service, interceptorChain);
        return (TService)proxy;
    }

    private void Initialize(TService service, IInterceptorChain interceptorChain)
    {
        _targetService = service ?? throw new ArgumentNullException(nameof(service));
        _interceptorChain = interceptorChain ?? throw new ArgumentNullException(nameof(interceptorChain));
    }

    /// <summary>
    /// Invokes the method on the target service, applying any interceptors.
    /// </summary>
    /// <param name="targetMethod">The method to invoke.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The result of the method invocation.</returns>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (_targetService == null || _interceptorChain == null)
        {
            throw new InvalidOperationException("Proxy not initialized. Call Create method to create a properly initialized proxy.");
        }

        if (targetMethod == null)
        {
            throw new ArgumentNullException(nameof(targetMethod));
        }

        var actualArgs = args ?? Array.Empty<object>();
        var context = new MethodExecutionContext(_targetService, targetMethod, actualArgs);
        
        return _interceptorChain.Execute(context, () => targetMethod.Invoke(_targetService, actualArgs));
    }
}
