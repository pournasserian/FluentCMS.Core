namespace FluentCMS.Providers.Abstractions;

public interface IProviderManager
{
    Task<ProviderCatalog> Add(ProviderCatalog providerCatalog, CancellationToken cancellationToken = default!);
    Task<IProviderModule?> GetProviderModule(string area, string typeName, CancellationToken cancellationToken = default!);
    Task<ProviderCatalog?> GetActiveByArea(string area, CancellationToken cancellationToken = default);
}