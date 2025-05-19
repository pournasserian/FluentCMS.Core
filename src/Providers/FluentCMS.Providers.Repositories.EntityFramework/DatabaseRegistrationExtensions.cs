using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Repositories.EntityFramework;

public static class DatabaseRegistrationExtensions
{
    public static IServiceCollection AddProviderSystem(this IServiceCollection services, string connectionString, string[] namespacePrefixes)
    {
        var tempServices = new ServiceCollection();
        tempServices.AddDbContext<ProvidersDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });
        tempServices.AddScoped<IProviderSystemServices, ProviderSystemServices>(); 
        tempServices.AddScoped<IApplicationExecutionContext, SystemExecutionContext>();

        using (var sp = tempServices.BuildServiceProvider())
        {
            var providerScanner = new ProviderScanner(null, namespacePrefixes);
            var providerInterfaces = providerScanner.FindProviders();

            var dbContext = sp.GetRequiredService<ProvidersDbContext>();

            // Add this check to avoid conflicts
            if (!dbContext.Database.CanConnect())
            {
                dbContext.Database.EnsureCreated();
            }
            try
            {
                // For subsequent DbContexts, we need a different approach
                // This will ensure the tables for this specific DbContext are created
                // without dropping existing tables
                var script = dbContext.Database.GenerateCreateScript();
                dbContext.Database.ExecuteSqlRaw(script);
            }
            catch (Exception)
            {
            }

            var providerService = sp.GetRequiredService<IProviderSystemServices>();
            providerService.Initialize(providerInterfaces).GetAwaiter().GetResult();
        }

        return services;
    }
}


public interface IProviderSystemServices
{
    Task Initialize(IEnumerable<ProviderScannerInterfaceMetaData> providerScannerInterfacesMetaData, CancellationToken cancellationToken = default);
}


public class ProviderSystemServices : IProviderSystemServices
{
    private readonly ProvidersDbContext _dbContext;
    private readonly IApplicationExecutionContext _applicationExecutionContext;

    public ProviderSystemServices(ProvidersDbContext dbContext, IApplicationExecutionContext applicationExecutionContext)
    {
        // check if database exists and is created
        dbContext.Database.EnsureCreated();

        // chekc if tables exist and are created
        if (!dbContext.Database.GetPendingMigrations().Any())
        {
            dbContext.Database.Migrate();
        }

        _dbContext = dbContext;
        _applicationExecutionContext = applicationExecutionContext;
    }

    public async Task Initialize(IEnumerable<ProviderScannerInterfaceMetaData> providerScannerInterfacesMetaData, CancellationToken cancellationToken = default)
    {
        var allTypesDict = _dbContext.Providerypes.ToDictionary(x => x.TypeName, x => x);
        var allImplementationsDict = _dbContext.ProviderImplementations.ToDictionary(x => x.TypeName, x => x);

        foreach (var providerScannerInterfaceMetaData in providerScannerInterfacesMetaData)
        {
            if (!allTypesDict.TryGetValue(providerScannerInterfaceMetaData.TypeName, out var providerType))
            {
                providerType = new ProviderType
                {
                    Id = Guid.NewGuid(),
                    TypeName = providerScannerInterfaceMetaData.TypeName,
                    AssemblyFile = providerScannerInterfaceMetaData.AssemblyFile,
                    AssemblyName = providerScannerInterfaceMetaData.AssemblyName,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _applicationExecutionContext.Username,
                    Version = 1,
                };
                await _dbContext.Providerypes.AddAsync(providerType, cancellationToken);
            }

            foreach (var implementation in providerScannerInterfaceMetaData.Implementations)
            {
                if (!allImplementationsDict.TryGetValue(implementation.TypeName, out var providerImplementation))
                {
                    providerImplementation = new ProviderImplementation
                    {
                        Id = Guid.NewGuid(),
                        TypeName = implementation.TypeName,
                        AssemblyFile = implementation.AssemblyFile,
                        AssemblyName = implementation.AssemblyName,
                        Name = implementation.TypeName,
                        IsActive = false,
                        IsInstalled = false,
                        ImplemetationVersion = implementation.Version,
                        Description = implementation.Description,
                        Category = implementation.Category,
                        IsDefault = implementation.IsDefault,
                        ProviderTypeId = providerType.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = _applicationExecutionContext.Username,
                        Version = 1,
                    };
                    await _dbContext.ProviderImplementations.AddAsync(providerImplementation, cancellationToken);
                }
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
