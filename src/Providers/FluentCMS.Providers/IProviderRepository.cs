namespace FluentCMS.Providers;

public interface IProviderRepository
{
    Task Add(Provider provider, CancellationToken cancellationToken = default);
    Task Update(Provider provider, CancellationToken cancellationToken = default);
    Task Remove(Provider provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<Provider>> GetAll(CancellationToken cancellationToken = default);
}