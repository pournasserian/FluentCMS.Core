using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Providers;

public static class ProviderRegistrationExtensions
{
    public static void AddProviders(this IHostApplicationBuilder builder, string[] providerPrefixes)
    {
        var providerManager = new ProviderManager(providerPrefixes, builder.Configuration);
        builder.Services.AddSingleton<IProviderManager>(providerManager);
        providerManager.ConfigureServices(builder);
    }
}
