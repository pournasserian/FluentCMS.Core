using FluentCMS.Providers.Caching.Abstractions;
using FluentCMS.Repositories.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Options.Repositories;

public interface IOptionsRepository : IRepository<OptionsDbModel>
{
    Task<OptionsDbModel?> GetByAlias(string alias, CancellationToken cancellationToken = default);
    Task<OptionsDbModel?> GetByAliasType(string alias, string typeName, CancellationToken cancellationToken = default);
}

internal class OptionsRepository(OptionsDbContext context, ICacheProvider cacheProvider, ILogger<OptionsRepository> logger) : CachedReporitory<OptionsDbModel, OptionsDbContext>(context, cacheProvider, logger), IOptionsRepository
{
    // TODO: implement caching for options by key
    public async Task<OptionsDbModel?> GetByAlias(string alias, CancellationToken cancellationToken = default)
    {
        var results = await GetAll(cancellationToken);
        return results.FirstOrDefault(o => string.Equals(o.Alias, alias, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<OptionsDbModel?> GetByAliasType(string alias, string typeName, CancellationToken cancellationToken = default)
    {
        var results = await GetAll(cancellationToken);
        return results.FirstOrDefault(o => string.Equals(o.Alias, alias, StringComparison.OrdinalIgnoreCase) && o.Type == typeName);
    }
}
