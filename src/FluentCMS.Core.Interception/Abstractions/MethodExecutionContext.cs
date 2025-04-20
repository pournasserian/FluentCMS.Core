using System.Reflection;

namespace FluentCMS.Core.Interception.Abstractions;

/// <summary>
/// Represents the context of a method execution, providing information about the method being executed and allowing interceptors to modify the execution flow.
/// </summary>
public class MethodExecutionContext
{
    /// <summary>
    /// Gets the method being executed.
    /// </summary>
    public MethodInfo Method { get; }
    
    /// <summary>
    /// Gets the arguments passed to the method.
    /// </summary>
    public object?[] Arguments { get; }
    
    /// <summary>
    /// Gets or sets the result of the method execution.
    /// </summary>
    public object? Result { get; set; }
    
    /// <summary>
    /// Gets or sets the exception that occurred during method execution.
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Gets the target instance on which the method is being executed.
    /// </summary>
    public object TargetInstance { get; }
    
    /// <summary>
    /// Gets the type of the target instance.
    /// </summary>
    public Type TargetType { get; }
    
    /// <summary>
    /// Gets a dictionary of additional data that can be used by interceptors.
    /// </summary>
    public Dictionary<string, object> Items { get; } = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MethodExecutionContext"/> class.
    /// </summary>
    /// <param name="targetInstance">The target instance on which the method is being executed.</param>
    /// <param name="method">The method being executed.</param>
    /// <param name="args">The arguments passed to the method.</param>
    public MethodExecutionContext(object targetInstance, MethodInfo method, object?[] args)
    {
        TargetInstance = targetInstance;
        TargetType = targetInstance.GetType();
        Method = method;
        Arguments = args;
    }
    
    /// <summary>
    /// Gets an argument from the argument list, cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the argument to.</typeparam>
    /// <param name="index">The index of the argument.</param>
    /// <returns>The argument, cast to the specified type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public T GetArgument<T>(int index)
    {
        if (index < 0 || index >= Arguments.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
                
        return (T)Arguments[index];
    }
}
