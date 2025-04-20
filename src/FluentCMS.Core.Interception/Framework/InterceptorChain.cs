using FluentCMS.Core.Interception.Abstractions;

namespace FluentCMS.Core.Interception.Framework;

/// <summary>
/// Default implementation of the interceptor chain that executes a series of interceptors around a method call.
/// </summary>
public class InterceptorChain : IInterceptorChain
{
    private readonly IEnumerable<IMethodInterceptor> _interceptors;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptorChain"/> class.
    /// </summary>
    /// <param name="interceptors">The interceptors to execute.</param>
    public InterceptorChain(IEnumerable<IMethodInterceptor> interceptors)
    {
        _interceptors = interceptors.OrderBy(i => i.Order);
    }
    
    /// <inheritdoc />
    public object? Execute(MethodExecutionContext context, Func<object?> proceedWithExecution)
    {
        try
        {
            // Execute before interceptors
            foreach (var interceptor in _interceptors)
            {
                interceptor.BeforeExecute(context);
            }
            
            // Execute the actual method
            var result = proceedWithExecution();
            context.Result = result;
            
            // Execute after interceptors in reverse order
            foreach (var interceptor in _interceptors.Reverse())
            {
                interceptor.AfterExecute(context);
            }
            
            return context.Result;
        }
        catch (Exception ex)
        {
            context.Exception = ex;
            
            // Execute exception handlers
            foreach (var interceptor in _interceptors)
            {
                interceptor.OnException(context);
            }
            
            throw context.Exception;
        }
    }
}
