using Microsoft.Extensions.Configuration;

namespace FluentCMS.Providers.Repositories.Configuration;

internal sealed class ConfigurationReadOnlyProviderRepository(IConfiguration configuration) : IProviderRepository
{
    public Task<IEnumerable<Provider>> GetAll(CancellationToken cancellationToken = default)
    {
        var providerRoot = configuration.GetSection("Providers").Get<ProviderConfigurationRoot>() ?? [];
        var providers = new List<Provider>();
        foreach (var (area, providerAreas) in providerRoot)
        {
            foreach (var providerArea in providerAreas)
            {
                providers.Add(new Provider
                {
                    Area = area,
                    Name = providerArea.Name,
                    DisplayName = providerArea.Name,
                    IsActive = providerArea.Active ?? false,
                    ModuleType = providerArea.Module,
                    Options = providerArea.Options is not null ? System.Text.Json.JsonSerializer.Serialize(providerArea.Options) : "{}"
                });
            }
        }
        return Task.FromResult<IEnumerable<Provider>>(providers);
    }

    public Task Add(Provider provider, CancellationToken cancellationToken = default)
    {
        throw new Exception("In-memory repository does not support adding providers.");
    }

    public Task Remove(Provider provider, CancellationToken cancellationToken = default)
    {
        throw new Exception("In-memory repository does not support removing providers.");
    }

    public Task Update(Provider provider, CancellationToken cancellationToken = default)
    {
        throw new Exception("In-memory repository does not support updating providers.");
    }
}

internal class ProviderConfigurationRoot : Dictionary<string, List<ProviderAreaConfiguration>>
{
}

internal class ProviderAreaConfiguration
{
    public string Name { get; set; } = string.Empty;
    public bool? Active { get; set; }
    public string Module { get; set; } = string.Empty;
    public Dictionary<string, object?>? Options { get; set; } // stays as dictionary
}
