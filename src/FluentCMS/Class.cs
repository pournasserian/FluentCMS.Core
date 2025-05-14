using System.Text.Json;
using FluentCMS.Repositories.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FluentCMS;

// Entity model for storing options in SQLite
public class OptionEntry : AuditableEntity
{
    public string TypeName { get; set; } = default!;
    public string Value { get; set; } = default!;
}

// DbContext for accessing the SQLite database
public class OptionsDbContext(DbContextOptions<OptionsDbContext> options) : BaseDbContext(options)
{
    public DbSet<OptionEntry> Options { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OptionEntry>()
            .HasIndex(o => o.TypeName)
            .IsUnique();
    }
}

public interface IOptionRepository: IRepository<OptionEntry>
{
    Task<OptionEntry?> GetByTypeName(string typeName, CancellationToken cancellationToken = default);
}

public class OptionRepository
    (OptionsDbContext dbContext, ILogger<OptionRepository> logger) :
    Repository<OptionEntry, OptionsDbContext>(dbContext, logger),
    IOptionRepository
{
    public async Task<OptionEntry?> GetByTypeName(string typeName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Options.FirstOrDefaultAsync(o => o.TypeName == typeName, cancellationToken);
    }
}

// Custom options provider that reads from SQLite
public class OptionsProvider<TOptions>(IOptionRepository optionRepository, IOptions<TOptions> defaultOptions) : IConfigureOptions<TOptions> where TOptions : class, new()
{
    public void Configure(TOptions options)
    {
        string typeName = typeof(TOptions).FullName ?? 
            throw new InvalidOperationException($"Type fullname for {typeof(TOptions).Name} cannot be null");

        // Try to get options from database
        var optionEntry = optionRepository.Find(o => o.TypeName == typeName).Result.FirstOrDefault();

        if (optionEntry == null)
        {
            // Insert default options if they don't exist
            var defaultValue = defaultOptions.Value;
            var jsonOptions = JsonSerializer.Serialize(defaultValue);

            optionEntry = new OptionEntry
            {
                TypeName = typeName,
                Value = jsonOptions
            };

            dbContext.Options.Add(optionEntry);
            dbContext.SaveChanges();

            // Copy default values to the options instance
            CopyValues(defaultValue, options);
        }
        else
        {
            // Deserialize options from database
            var dbOptions = JsonSerializer.Deserialize<TOptions>(optionEntry.Value);

            // Copy values from database to the options instance
            CopyValues(dbOptions, options);
        }
    }

    private void CopyValues(TOptions source, TOptions destination)
    {
        // Serialize and deserialize to handle nested properties
        var json = JsonSerializer.Serialize(source);
        var tempOptions = JsonSerializer.Deserialize<TOptions>(json);

        // Copy all properties from tempOptions to destination
        var properties = typeof(TOptions).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(tempOptions);
            property.SetValue(destination, value);
        }
    }
}

// Extension methods for registering the SQLite options
public static class SqliteOptionsExtensions
{
    public static IServiceCollection AddSqliteOptions<TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureDefaults = null)
        where TOptions : class, new()
    {
        // Add regular options with defaults
        services.AddOptions<TOptions>()
            .Configure(options => configureDefaults?.Invoke(options));

        // Register the custom provider
        services.AddSingleton<IConfigureOptions<TOptions>, SqliteOptionsProvider<TOptions>>();

        return services;
    }
}