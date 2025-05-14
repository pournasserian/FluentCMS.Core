using FluentCMS.Repositories.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.ConfigOptionsProvider;

public interface IOptionRepository : IRepository<OptionEntry>
{
    Task<OptionEntry?> GetByTypeName(string typeName, CancellationToken cancellationToken = default);
}

public class OptionRepository(OptionsDbContext dbContext, ILogger<OptionRepository> logger) : Repository<OptionEntry, OptionsDbContext>(dbContext, logger), IOptionRepository
{
    public async Task<OptionEntry?> GetByTypeName(string typeName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Options.FirstOrDefaultAsync(o => o.TypeName == typeName, cancellationToken);
    }
}
