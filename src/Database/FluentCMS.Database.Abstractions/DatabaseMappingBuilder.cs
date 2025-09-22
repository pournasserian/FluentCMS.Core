using System.Reflection;

namespace FluentCMS.Database.Abstractions;

/// <summary>
/// Fluent builder for configuring database mappings with extensible provider support.
/// </summary>
internal sealed class DatabaseMappingBuilder : IDatabaseMappingBuilder, IAssemblyMappingBuilder
{
    private readonly DatabaseProviderMapping _configuration = new();
    private Assembly? _currentAssembly;
    private bool _isDefault;

    /// <summary>
    /// Configures the default database provider for assemblies that don't have explicit mappings.
    /// </summary>
    public IAssemblyMappingBuilder SetDefault()
    {
        _isDefault = true;
        _currentAssembly = null;
        return this;
    }

    /// <summary>
    /// Maps a specific assembly to use a particular database provider.
    /// </summary>
    public IAssemblyMappingBuilder MapAssembly<T>()
    {
        _isDefault = false;
        _currentAssembly = typeof(T).Assembly;
        return this;
    }

    /// <summary>
    /// Maps a specific assembly to use a particular database provider.
    /// </summary>
    public IAssemblyMappingBuilder MapAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        _isDefault = false;
        _currentAssembly = assembly;
        return this;
    }

    /// <summary>
    /// Registers a database provider for the current assembly mapping.
    /// This method is used internally by extension methods from database provider libraries.
    /// </summary>
    public IAssemblyMappingBuilder RegisterProvider(string providerName, string connectionString, Func<string, IServiceProvider, IDatabaseManager> factory)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        if (_isDefault)
        {
            _configuration.SetDefault(providerName, connectionString, factory);
        }
        else if (_currentAssembly != null)
        {
            _configuration.MapAssembly(_currentAssembly, providerName, connectionString, factory);
        }
        else
        {
            throw new InvalidOperationException("No assembly specified for mapping. Call SetDefault() or MapAssembly() first.");
        }

        return this;
    }

    /// <summary>
    /// Builds the configuration. Internal use only.
    /// </summary>
    internal DatabaseProviderMapping Build() => _configuration;
}
