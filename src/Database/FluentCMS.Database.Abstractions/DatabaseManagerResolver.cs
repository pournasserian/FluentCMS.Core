using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace FluentCMS.Database.Abstractions;

/// <summary>
/// Resolves the appropriate IDatabaseManager based on the calling assembly using registered providers.
/// </summary>
internal sealed class DatabaseManagerResolver : IDatabaseManager
{
    private readonly DatabaseProviderMapping _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseManagerResolver> _logger;

    public DatabaseManagerResolver(
        DatabaseProviderMapping configuration,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DatabaseManagerResolver>();
    }

    public async Task<bool> DatabaseExists(CancellationToken cancellationToken = default)
    {
        var manager = GetDatabaseManager();
        return await manager.DatabaseExists(cancellationToken);
    }

    public async Task CreateDatabase(CancellationToken cancellationToken = default)
    {
        var manager = GetDatabaseManager();
        await manager.CreateDatabase(cancellationToken);
    }

    public async Task<bool> TablesExist(IEnumerable<string> tableNames, CancellationToken cancellationToken = default)
    {
        var manager = GetDatabaseManager();
        return await manager.TablesExist(tableNames, cancellationToken);
    }

    public async Task<bool> TablesEmpty(IEnumerable<string> tableNames, CancellationToken cancellationToken = default)
    {
        var manager = GetDatabaseManager();
        return await manager.TablesEmpty(tableNames, cancellationToken);
    }

    public async Task ExecuteRaw(string sql, CancellationToken cancellationToken = default)
    {
        var manager = GetDatabaseManager();
        await manager.ExecuteRaw(sql, cancellationToken);
    }

    /// <summary>
    /// Gets the appropriate database manager based on the calling assembly.
    /// </summary>
    private IDatabaseManager GetDatabaseManager()
    {
        var callingAssembly = GetCallingAssembly();
        var providerConfig = _configuration.GetConfiguration(callingAssembly);

        if (providerConfig == null)
        {
            var assemblyName = callingAssembly.GetName().Name ?? "Unknown";
            throw new InvalidOperationException(
                $"No database provider configuration found for assembly '{assemblyName}' and no default configuration is set. " +
                "Please configure a default database provider or add a specific mapping for this assembly.");
        }

        _logger.LogDebug("Resolved database for assembly '{AssemblyName}': Provider='{ProviderName}', ConnectionString='{ConnectionString}'",
            callingAssembly.GetName().Name, providerConfig.ProviderName, providerConfig.ConnectionString);

        try
        {
            return providerConfig.Factory(providerConfig.ConnectionString, _serviceProvider);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create database manager for provider '{providerConfig.ProviderName}'. " +
                $"Make sure the provider library is referenced and properly configured.", ex);
        }
    }

    /// <summary>
    /// Determines the calling assembly by walking up the stack trace.
    /// </summary>
    private Assembly GetCallingAssembly()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        var stackTrace = new StackTrace();
        var frames = stackTrace.GetFrames();

        if (frames == null)
        {
            // Fallback to entry assembly if stack trace is not available
            return Assembly.GetEntryAssembly() ?? currentAssembly;
        }

        // Walk up the stack to find the first method from a different assembly
        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method?.DeclaringType?.Assembly is Assembly assembly && 
                assembly != currentAssembly &&
                !IsSystemAssembly(assembly))
            {
                return assembly;
            }
        }

        // Fallback to entry assembly if no suitable assembly is found
        return Assembly.GetEntryAssembly() ?? currentAssembly;
    }

    /// <summary>
    /// Checks if the assembly is a system assembly that should be ignored.
    /// </summary>
    private static bool IsSystemAssembly(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        if (string.IsNullOrEmpty(assemblyName))
            return true;

        // Skip common system assemblies
        return assemblyName.StartsWith("System.") ||
               assemblyName.StartsWith("Microsoft.") ||
               assemblyName.StartsWith("netstandard") ||
               assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase);
    }
}
