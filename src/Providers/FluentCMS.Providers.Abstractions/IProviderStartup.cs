using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Abstractions;

public interface IProviderStartup
{
    public string Area { get; }
    public Type Interface { get; }
    public Type Implementation { get; }

    void Register(IServiceCollection services, IConfiguration? settingsSection);
}