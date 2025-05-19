namespace FluentCMS.Providers;

// Interface for plugin contexts
public interface IProviderContext
{
    IServiceScope CreateScope();
    IDisposable PreventUnload();
    bool IsDisposed { get; }
}

public class ProviderContext(AssemblyContext context) : IProviderContext
{
    public IServiceScope CreateScope()
    {
        return context.CreateScope();
    }

    public IDisposable PreventUnload()
    {
        return context.PreventUnload();
    }

    public bool IsDisposed => context == null;
}