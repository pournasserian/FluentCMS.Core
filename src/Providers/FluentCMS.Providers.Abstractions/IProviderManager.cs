namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Interface for managing providers in the system.
/// </summary>
public interface IProviderManager
{
    /// <summary>
    /// Gets all available provider types in the system.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns a collection of provider type information.</returns>
    Task<IEnumerable<ProviderTypeInfo>> GetProviderTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all provider implementations for a specific provider type.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns a collection of provider implementation information.</returns>
    Task<IEnumerable<ProviderImplementationInfo>> GetImplementationsAsync<TProvider>(CancellationToken cancellationToken = default) 
        where TProvider : IProvider;

    /// <summary>
    /// Gets all provider implementations for a specific provider type.
    /// </summary>
    /// <param name="providerTypeId">The ID of the provider type.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns a collection of provider implementation information.</returns>
    Task<IEnumerable<ProviderImplementationInfo>> GetImplementationsAsync(string providerTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active provider implementation for a specific provider type.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns the active provider implementation or null if none is active.</returns>
    Task<TProvider?> GetActiveProviderAsync<TProvider>(CancellationToken cancellationToken = default) 
        where TProvider : IProvider;

    /// <summary>
    /// Gets the active provider implementation information for a specific provider type.
    /// </summary>
    /// <param name="providerTypeId">The ID of the provider type.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns the active provider implementation information or null if none is active.</returns>
    Task<ProviderImplementationInfo?> GetActiveImplementationAsync(string providerTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active provider implementation for a specific provider type.
    /// </summary>
    /// <param name="providerTypeId">The ID of the provider type.</param>
    /// <param name="implementationId">The ID of the provider implementation to activate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the activation operation.</returns>
    Task SetActiveImplementationAsync(string providerTypeId, string implementationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration for a specific provider implementation.
    /// </summary>
    /// <typeparam name="TOptions">The type of options for the provider.</typeparam>
    /// <param name="implementationId">The ID of the provider implementation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns the provider configuration or a new instance if none exists.</returns>
    Task<TOptions> GetConfigurationAsync<TOptions>(string implementationId, CancellationToken cancellationToken = default) 
        where TOptions : class, new();

    /// <summary>
    /// Updates the configuration for a specific provider implementation.
    /// </summary>
    /// <typeparam name="TOptions">The type of options for the provider.</typeparam>
    /// <param name="implementationId">The ID of the provider implementation.</param>
    /// <param name="options">The new configuration options.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the update operation.</returns>
    Task UpdateConfigurationAsync<TOptions>(string implementationId, TOptions options, CancellationToken cancellationToken = default) 
        where TOptions : class, new();

    /// <summary>
    /// Installs a new provider implementation from an assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly containing the provider implementation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns information about the installed provider implementation.</returns>
    Task<ProviderImplementationInfo> InstallProviderAsync(string assemblyPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls a provider implementation.
    /// </summary>
    /// <param name="implementationId">The ID of the provider implementation to uninstall.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the uninstallation operation.</returns>
    Task UninstallProviderAsync(string implementationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of a specific provider implementation.
    /// </summary>
    /// <param name="implementationId">The ID of the provider implementation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns the health status and a status message.</returns>
    Task<(ProviderHealthStatus Status, string Message)> CheckHealthAsync(string implementationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the provider registry by scanning for new provider implementations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the refresh operation.</returns>
    Task RefreshProviderRegistryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a provider type.
/// </summary>
public class ProviderTypeInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider type.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the provider type.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the full .NET type name of the provider interface.
    /// </summary>
    public string FullTypeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the assembly name containing the provider interface.
    /// </summary>
    public string AssemblyName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the creation timestamp of the provider type.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp of the provider type.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Information about a provider implementation.
/// </summary>
public class ProviderImplementationInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider implementation.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider type ID.
    /// </summary>
    public string ProviderTypeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the provider implementation.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the provider implementation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the version of the provider implementation.
    /// </summary>
    public string Version { get; set; } = null!;

    /// <summary>
    /// Gets or sets the full .NET type name of the provider implementation.
    /// </summary>
    public string FullTypeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path to the assembly containing the provider implementation.
    /// </summary>
    public string AssemblyPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the provider implementation is installed.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider implementation is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the health status of the provider implementation.
    /// </summary>
    public ProviderHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// Gets or sets the health status message of the provider implementation.
    /// </summary>
    public string? HealthMessage { get; set; }

    /// <summary>
    /// Gets or sets the last health check timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset? LastHealthCheckAt { get; set; }

    /// <summary>
    /// Gets or sets the installation timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset? InstalledAt { get; set; }

    /// <summary>
    /// Gets or sets the activation timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset? ActivatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
