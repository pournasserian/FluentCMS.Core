using System.Reflection;

namespace FluentCMS.Database.Abstractions;

/// <summary>
/// Configuration for mapping assemblies to database providers using factory functions.
/// </summary>
internal sealed class DatabaseProviderMapping
{
    private readonly Dictionary<string, ProviderConfiguration> _assemblyMappings = new();
    private ProviderConfiguration? _defaultMapping;

    /// <summary>
    /// Sets the default database provider configuration for unmapped assemblies.
    /// </summary>
    public void SetDefault(string providerName, string connectionString, Func<string, IServiceProvider, IDatabaseManager> factory)
    {
        _defaultMapping = new ProviderConfiguration(providerName, connectionString, factory);
    }

    /// <summary>
    /// Maps an assembly to a specific database provider configuration.
    /// </summary>
    public void MapAssembly(Assembly assembly, string providerName, string connectionString, Func<string, IServiceProvider, IDatabaseManager> factory)
    {
        var assemblyName = assembly.GetName().Name;
        if (string.IsNullOrEmpty(assemblyName))
            throw new ArgumentException("Assembly name cannot be null or empty.", nameof(assembly));

        _assemblyMappings[assemblyName] = new ProviderConfiguration(providerName, connectionString, factory);
    }

    /// <summary>
    /// Gets the provider configuration for the specified assembly.
    /// Returns the default configuration if no specific mapping is found.
    /// </summary>
    public ProviderConfiguration? GetConfiguration(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        if (string.IsNullOrEmpty(assemblyName))
            return _defaultMapping;

        return _assemblyMappings.TryGetValue(assemblyName, out var configuration) ? configuration : _defaultMapping;
    }

    /// <summary>
    /// Gets the default provider configuration.
    /// </summary>
    public ProviderConfiguration? GetDefaultConfiguration() => _defaultMapping;
}

/// <summary>
/// Represents a database provider configuration with factory function.
/// </summary>
internal sealed record ProviderConfiguration(
    string ProviderName, 
    string ConnectionString, 
    Func<string, IServiceProvider, IDatabaseManager> Factory);
