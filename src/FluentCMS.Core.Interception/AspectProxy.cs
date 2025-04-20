using System.Reflection;

namespace FluentCMS.Core.Interception;

public class AspectProxy : DispatchProxy
{
    private object _target;
    private IServiceProvider _serviceProvider;
    private List<InterceptorRegistration> _interceptorRegistrations;

    /// <summary>
    /// Initializes the proxy with the target instance and service provider
    /// </summary>
    public void Initialize(object target, IServiceProvider serviceProvider, List<InterceptorRegistration> interceptorRegistrations)
    {
        _target = target;
        _serviceProvider = serviceProvider;
        _interceptorRegistrations = interceptorRegistrations;
    }

    /// <summary>
    /// Gets applicable interceptors for a method
    /// </summary>
    private List<IAspectInterceptor> GetInterceptorsForMethod(MethodInfo method)
    {
        return _interceptorRegistrations
            .Where(r => r.MethodFilter(method))
            .SelectMany(r => r.Interceptors)
            .OrderBy(i => i.Order)
            .ToList();
    }

    /// <summary>
    /// Invokes the method with all applicable interceptors
    /// </summary>
    protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        // If the target or method is null, return null
        if (_target == null || targetMethod == null)
            return null;

        // Find the implementation method
        var implementationMethod = _target.GetType().GetMethod(
            targetMethod.Name,
            targetMethod.GetParameters().Select(p => p.ParameterType).ToArray());

        if (implementationMethod == null)
            return null;

        // Get all applicable interceptors for this method
        var interceptors = GetInterceptorsForMethod(implementationMethod);

        // Check if this is an async method
        bool isAsync = typeof(Task).IsAssignableFrom(targetMethod.ReturnType) ||
                      (targetMethod.ReturnType.IsGenericType &&
                       targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

        if (!interceptors.Any())
        {
            // If no interceptors, just invoke the method directly
            return implementationMethod.Invoke(_target, args);
        }

        if (isAsync)
        {
            // Handle async method
            return InvokeAsync(implementationMethod, args, interceptors);
        }
        else
        {
            // Handle synchronous method
            return InvokeSync(implementationMethod, args, interceptors);
        }
    }

    /// <summary>
    /// Handles synchronous method invocation with interceptors
    /// </summary>
    private object InvokeSync(MethodInfo? method, object?[]? args, List<IAspectInterceptor> interceptors)
    {
        object result = null;
        try
        {
            // Call OnBefore for all interceptors
            foreach (var interceptor in interceptors)
            {
                interceptor.OnBefore(method, args, _target);
            }

            // Invoke the target method
            result = method.Invoke(_target, args);

            // Process return value through all interceptors
            foreach (var interceptor in interceptors)
            {
                result = interceptor.OnReturn(method, args, _target, result);
            }

            // Call OnAfter for all interceptors
            foreach (var interceptor in interceptors)
            {
                interceptor.OnAfter(method, args, _target, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            // Unwrap TargetInvocationException
            var actualException = ex is TargetInvocationException ? ex.InnerException : ex;

            // Call OnException for all interceptors
            foreach (var interceptor in interceptors)
            {
                interceptor.OnException(method, args, _target, actualException);
            }

            throw actualException;
        }
    }

    /// <summary>
    /// Handles asynchronous method invocation with interceptors
    /// </summary>
    private object InvokeAsync(MethodInfo? method, object?[]? args, List<IAspectInterceptor> interceptors)
    {
        // Determine if the method returns Task or Task<T>
        bool isTaskWithResult = method.ReturnType.IsGenericType &&
                               method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);

        Type taskResultType = isTaskWithResult ? method.ReturnType.GetGenericArguments()[0] : null;

        try
        {
            // Call OnBefore for all interceptors
            foreach (var interceptor in interceptors)
            {
                interceptor.OnBefore(method, args, _target);
            }

            // Invoke the async method
            var task = (Task)method.Invoke(_target, args);

            // Create a TaskCompletionSource to create the result task
            dynamic tcs;
            if (isTaskWithResult)
            {
                var tcsType = typeof(TaskCompletionSource<>).MakeGenericType(taskResultType);
                tcs = Activator.CreateInstance(tcsType);
            }
            else
            {
                tcs = new TaskCompletionSource<object>();
            }

            // Continue with the task
            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        // Get the actual exception
                        var exception = t.Exception.InnerExceptions.Count == 1
                            ? t.Exception.InnerExceptions[0]
                            : t.Exception;

                        // Call OnException for all interceptors
                        foreach (var interceptor in interceptors)
                        {
                            interceptor.OnException(method, args, _target, exception);
                        }

                        // Set the exception on the TCS
                        tcs.SetException(exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        // Task completed successfully
                        object resultValue = null;

                        if (isTaskWithResult)
                        {
                            // Get the result from the Task<T>
                            var resultProperty = t.GetType().GetProperty("Result");
                            resultValue = resultProperty.GetValue(t);

                            // Process return value through all interceptors
                            foreach (var interceptor in interceptors)
                            {
                                resultValue = interceptor.OnReturn(method, args, _target, resultValue);
                            }

                            // Call OnAfter for all interceptors
                            foreach (var interceptor in interceptors)
                            {
                                interceptor.OnAfter(method, args, _target, resultValue);
                            }

                            // Set the result on the TCS
                            tcs.SetResult(resultValue);
                        }
                        else
                        {
                            // For Task (no result)
                            // Call OnAfter for all interceptors
                            foreach (var interceptor in interceptors)
                            {
                                interceptor.OnAfter(method, args, _target, null);
                            }

                            // Complete the TCS
                            tcs.SetResult(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Set the exception on the TCS
                    tcs.SetException(ex);
                }
            });

            // Return the TCS's Task
            return tcs.Task;
        }
        catch (Exception ex)
        {
            // Unwrap TargetInvocationException
            var actualException = ex is TargetInvocationException ? ex.InnerException : ex;

            // Call OnException for all interceptors
            foreach (var interceptor in interceptors)
            {
                interceptor.OnException(method, args, _target, actualException);
            }

            // Create a failed task
            if (isTaskWithResult)
            {
                var tcsType = typeof(TaskCompletionSource<>).MakeGenericType(taskResultType);
                dynamic tcs = Activator.CreateInstance(tcsType);
                tcs.SetException(actualException);
                return tcs.Task;
            }
            else
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(actualException);
                return tcs.Task;
            }
        }
    }
}