using FluentCMS.Database.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Database.Extensions;

/// <summary>
/// Fluent builder for configuring library-based database mappings that registers services directly in DI container.
/// This eliminates the need for runtime reflection and provides high-performance database resolution.
/// </summary>
internal sealed class LibraryDatabaseMappingBuilder(IServiceCollection services) : ILibraryDatabaseMappingBuilder, ILibraryMappingBuilder
{
    private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));
    private Type? _currentMarkerType;
    private bool _isDefault;
    private bool _hasDefaultConfiguration;

    /// <summary>
    /// Gets whether a default configuration has been set.
    /// </summary>
    public bool HasDefaultConfiguration => _hasDefaultConfiguration;

    /// <summary>
    /// Configures the default database provider for libraries that don't have explicit mappings.
    /// </summary>
    public ILibraryMappingBuilder SetDefault()
    {
        _isDefault = true;
        _currentMarkerType = typeof(IDefaultLibraryMarker);
        return this;
    }

    /// <summary>
    /// Maps a specific library marker to use a particular database provider.
    /// </summary>
    public ILibraryMappingBuilder MapLibrary<TLibraryMarker>() where TLibraryMarker : IDatabaseManagerMarker
    {
        _isDefault = false;
        _currentMarkerType = typeof(TLibraryMarker);
        return this;
    }

    /// <summary>
    /// Registers a database provider for the current library mapping.
    /// </summary>
    public ILibraryMappingBuilder RegisterProvider(string providerName, string connectionString, Func<string, IServiceProvider, IDatabaseManager> factory)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        if (_currentMarkerType == null)
            throw new InvalidOperationException("No library marker specified for mapping. Call SetDefault() or MapLibrary() first.");

        // Register specific library marker service
        var serviceType = typeof(IDatabaseManager<>).MakeGenericType(_currentMarkerType);
        _services.AddScoped(serviceType, serviceProvider =>
        {
            var databaseManager = factory(connectionString, serviceProvider);
            var typedManagerType = typeof(TypedDatabaseManager<>).MakeGenericType(_currentMarkerType);
            return Activator.CreateInstance(typedManagerType, databaseManager)!;
        });

        if (_isDefault)
        {
            _hasDefaultConfiguration = true;
        }

        return this;
    }
}
