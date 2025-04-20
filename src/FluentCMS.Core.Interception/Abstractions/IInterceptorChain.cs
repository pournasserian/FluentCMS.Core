namespace FluentCMS.Core.Interception.Abstractions;

/// <summary>
/// Defines a chain of interceptors that process method calls.
/// </summary>
public interface IInterceptorChain
{
    /// <summary>
    /// Executes the chain of interceptors around the specified method execution.
    /// </summary>
    /// <param name="context">The context of the method execution.</param>
    /// <param name="proceedWithExecution">A delegate that executes the target method.</param>
    /// <returns>The result of the method execution, which may be modified by interceptors.</returns>
    object? Execute(MethodExecutionContext context, Func<object?> proceedWithExecution);
}
