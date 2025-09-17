using Microsoft.Extensions.Logging;

namespace FluentCMS.Providers.Repositories.EntityFramework;

internal class ProviderRepository(ProviderDbContext dbContext, ILogger<ProviderRepository> logger) : IProviderRepository
{
    public Task Add(Provider provider, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Provider>> GetAll(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Remove(Provider provider, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Update(Provider provider, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
