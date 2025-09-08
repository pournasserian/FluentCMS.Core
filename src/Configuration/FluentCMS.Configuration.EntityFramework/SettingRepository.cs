using FluentCMS.Providers.Caching.Abstractions;
using FluentCMS.Repositories.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Configuration.EntityFramework;

public interface ISettingRepository : ICachedRepository<SettingDbModel>
{
}

public class SettingRepository(ConfigurationDbContext dbContext, ICacheProvider cacheProvider, ILogger<SettingRepository> logger) : CachedReporitory<SettingDbModel, ConfigurationDbContext>(dbContext, cacheProvider, logger), ISettingRepository
{
}