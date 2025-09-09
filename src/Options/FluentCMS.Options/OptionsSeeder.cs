using FluentCMS.DataSeeder.Abstractions;
using FluentCMS.Options.Repositories;
using FluentCMS.Options.Services;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Options;

public class OptionsSeeder(OptionsDbContext dbContext, IDatabaseManager databaseManager, IEnumerable<OptionsDescriptor> optionsDescriptors, IOptionsService optionsService) : ISeeder
{
    public int Order => 1;

    public async Task CreateSchema(CancellationToken cancellationToken = default)
    {
        await databaseManager.CreateDatabase(cancellationToken);
        var sql = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task SeedData(CancellationToken cancellationToken = default)
    {
        // Get all option aliases from the catalog
        foreach (var optionDescriptor in optionsDescriptors)
        {
            // Check if the option already exists
            if (!await optionsService.Exists(optionDescriptor.Alias, optionDescriptor.Type.FullName!, cancellationToken))
            {
                // If not, create it with the default value
                // Get default value from IOption instance
                await optionsService.Update(optionDescriptor.Alias, optionDescriptor.DefaultValue, cancellationToken);
            }
        }
    }

    public async Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        if (!await databaseManager.DatabaseExists(cancellationToken))
            return true;
        return !await databaseManager.TablesExist(["Options"], cancellationToken);
    }

    public Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        // Always attempt to seed options
        // Checking for each option's existence would be too complex here
        return Task.FromResult(true);
    }
}
