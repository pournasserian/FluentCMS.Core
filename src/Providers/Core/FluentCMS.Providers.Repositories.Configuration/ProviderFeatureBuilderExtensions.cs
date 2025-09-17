using FluentCMS.Providers.Repositories.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Repositories.Configuration;

public static class ProviderFeatureBuilderExtensions
{
    public static ProviderFeatureBuilder UseConfiguration(this ProviderFeatureBuilder providerFeatureBuilder)
    {
        providerFeatureBuilder.Services.AddScoped<IProviderRepository, ConfigurationReadOnlyProviderRepository>();

        return providerFeatureBuilder;
    }
}
