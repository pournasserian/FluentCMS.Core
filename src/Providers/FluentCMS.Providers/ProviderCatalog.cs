using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers;

public class ProviderCatalog(IProviderModule module, string providerName, bool active)
{
    public string Name { get; set; } = providerName;
    public bool Active { get; set; } = active;
    public IProviderModule Module { get; set; } = module;
}
