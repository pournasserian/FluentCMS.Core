namespace FluentCMS.Interception;

public class InterceptorRegistration
{
    public List<IAspectInterceptor> Interceptors { get; } = [];
    public Func<MethodInfo, bool> MethodFilter { get; set; } = _ => true;
    public InterceptorRegistration AddInterceptor(IAspectInterceptor interceptor)
    {
        Interceptors.Add(interceptor);
        return this;
    }
    public InterceptorRegistration WithMethodFilter(Func<MethodInfo, bool> filter)
    {
        MethodFilter = filter;
        return this;
    }
}