namespace FluentCMS.Providers;

// Interface for provider shutdown notification
public interface IProviderShutdownHandler
{
    void OnShutdown();
}
