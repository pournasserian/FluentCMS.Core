namespace FluentCMS.Interception;

public abstract class AspectInterceptorBase : IAspectInterceptor
{
    public virtual int Order { get; set; } = 0;
    public virtual void OnBefore(MethodInfo? method, object?[]? arguments, object instance) { }
    public virtual void OnAfter(MethodInfo? method, object?[]? arguments, object instance, object? returnValue) { }
    public virtual void OnException(MethodInfo? method, object?[]? arguments, object instance, Exception? exception) { }
    public virtual object? OnReturn(MethodInfo? method, object?[]? arguments, object instance, object? returnValue) => returnValue;
}