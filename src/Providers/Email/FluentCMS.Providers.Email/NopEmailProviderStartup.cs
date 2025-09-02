using FluentCMS.Providers.Abstractions;
using FluentCMS.Providers.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Email;

public class NopEmailProviderStartup : IProviderStartup
{
    public string Area => "Email";

    public Type Interface => typeof(IEmailProvider);

    public Type Implementation => typeof(NopEmailProvider);

    public void Register(IServiceCollection services, IConfiguration? settingsSection)
    {
        services.AddSingleton<IEmailProvider, NopEmailProvider>();
    }
}
