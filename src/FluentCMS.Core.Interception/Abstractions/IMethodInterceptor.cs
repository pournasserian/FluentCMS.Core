namespace FluentCMS.Core.Interception.Abstractions;

/// <summary>
/// Defines the contract for method interceptors that can modify the behavior of method calls.
/// </summary>
public interface IMethodInterceptor
{
    /// <summary>
    /// Gets the order in which this interceptor should be executed relative to other interceptors.
    /// Lower values indicate higher priority.
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Called before the target method is executed.
    /// </summary>
    /// <param name="context">The context of the method execution.</param>
    void BeforeExecute(MethodExecutionContext context);
    
    /// <summary>
    /// Called after the target method has been executed successfully.
    /// </summary>
    /// <param name="context">The context of the method execution.</param>
    void AfterExecute(MethodExecutionContext context);
    
    /// <summary>
    /// Called when an exception occurs during the execution of the target method.
    /// </summary>
    /// <param name="context">The context of the method execution.</param>
    void OnException(MethodExecutionContext context);
}
