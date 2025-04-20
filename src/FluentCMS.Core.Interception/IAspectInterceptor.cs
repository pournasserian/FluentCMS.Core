using System.Reflection;

namespace FluentCMS.Core.Interception;

public interface IAspectInterceptor
{
    int Order { get; }
    void OnBefore(MethodInfo? method, object?[]? arguments, object instance);
    void OnAfter(MethodInfo? method, object?[]? arguments, object instance, object? returnValue);
    void OnException(MethodInfo? method, object?[]? arguments, object instance, Exception? exception);
    object? OnReturn(MethodInfo? method, object?[]? arguments, object instance, object? returnValue);
}
