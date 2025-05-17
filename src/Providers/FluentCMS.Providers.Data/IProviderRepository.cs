using FluentCMS.Providers.Data.Models;

namespace FluentCMS.Providers.Data;

/// <summary>
/// Interface for repository operations on provider data.
/// </summary>
public interface IProviderRepository
{
    #region Provider Types

    /// <summary>
    /// Gets all provider types.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of provider types.</returns>
    Task<IEnumerable<ProviderType>> GetProviderTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a provider type by ID.
    /// </summary>
    /// <param name="providerTypeId">The provider type ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provider type, or null if not found.</returns>
    Task<ProviderType?> GetProviderTypeByIdAsync(string providerTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a provider type by full type name.
    /// </summary>
    /// <param name="fullTypeName">The full type name of the provider interface.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provider type, or null if not found.</returns>
    Task<ProviderType?> GetProviderTypeByFullTypeNameAsync(string fullTypeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new provider type.
    /// </summary>
    /// <param name="providerType">The provider type to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The added provider type.</returns>
    Task<ProviderType> AddProviderTypeAsync(ProviderType providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing provider type.
    /// </summary>
    /// <param name="providerType">The provider type to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated provider type.</returns>
    Task<ProviderType> UpdateProviderTypeAsync(ProviderType providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a provider type.
    /// </summary>
    /// <param name="providerTypeId">The provider type ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the provider type was deleted, false if it was not found.</returns>
    Task<bool> DeleteProviderTypeAsync(string providerTypeId, CancellationToken cancellationToken = default);

    #endregion

    #region Provider Implementations

    /// <summary>
    /// Gets all provider implementations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of provider implementations.</returns>
    Task<IEnumerable<ProviderImplementation>> GetProviderImplementationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all provider implementations for a specific provider type.
    /// </summary>
    /// <param name="providerTypeId">The provider type ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of provider implementations.</returns>
    Task<IEnumerable<ProviderImplementation>> GetProviderImplementationsByTypeAsync(string providerTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a provider implementation by ID.
    /// </summary>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provider implementation, or null if not found.</returns>
    Task<ProviderImplementation?> GetProviderImplementationByIdAsync(string implementationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a provider implementation by full type name.
    /// </summary>
    /// <param name="providerTypeId">The provider type ID.</param>
    /// <param name="fullTypeName">The full type name of the provider implementation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provider implementation, or null if not found.</returns>
    Task<ProviderImplementation?> GetProviderImplementationByFullTypeNameAsync(string providerTypeId, string fullTypeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active provider implementation for a specific provider type.
    /// </summary>
    /// <param name="providerTypeId">The provider type ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The active provider implementation, or null if none is active.</returns>
    Task<ProviderImplementation?> GetActiveProviderImplementationAsync(string providerTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new provider implementation.
    /// </summary>
    /// <param name="implementation">The provider implementation to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The added provider implementation.</returns>
    Task<ProviderImplementation> AddProviderImplementationAsync(ProviderImplementation implementation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing provider implementation.
    /// </summary>
    /// <param name="implementation">The provider implementation to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated provider implementation.</returns>
    Task<ProviderImplementation> UpdateProviderImplementationAsync(ProviderImplementation implementation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active provider implementation for a specific provider type.
    /// </summary>
    /// <param name="providerTypeId">The provider type ID.</param>
    /// <param name="implementationId">The provider implementation ID to activate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activated provider implementation.</returns>
    Task<ProviderImplementation> SetActiveProviderImplementationAsync(string providerTypeId, string implementationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a provider implementation.
    /// </summary>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the provider implementation was deleted, false if it was not found.</returns>
    Task<bool> DeleteProviderImplementationAsync(string implementationId, CancellationToken cancellationToken = default);

    #endregion

    #region Provider Configurations

    /// <summary>
    /// Gets a provider configuration by implementation ID.
    /// </summary>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provider configuration, or null if not found.</returns>
    Task<ProviderConfiguration?> GetProviderConfigurationAsync(string implementationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed provider configuration by implementation ID.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed provider configuration, or default if not found.</returns>
    Task<T?> GetTypedProviderConfigurationAsync<T>(string implementationId, CancellationToken cancellationToken = default) where T : class, new();

    /// <summary>
    /// Updates a provider configuration.
    /// </summary>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="configurationJson">The configuration JSON.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated provider configuration.</returns>
    Task<ProviderConfiguration> UpdateProviderConfigurationAsync(string implementationId, string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a typed provider configuration.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="configuration">The configuration object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated provider configuration.</returns>
    Task<ProviderConfiguration> UpdateTypedProviderConfigurationAsync<T>(string implementationId, T configuration, CancellationToken cancellationToken = default) where T : class, new();

    /// <summary>
    /// Deletes a provider configuration.
    /// </summary>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the provider configuration was deleted, false if it was not found.</returns>
    Task<bool> DeleteProviderConfigurationAsync(string implementationId, CancellationToken cancellationToken = default);

    #endregion

    #region Health Status

    /// <summary>
    /// Updates the health status of a provider implementation.
    /// </summary>
    /// <param name="implementationId">The provider implementation ID.</param>
    /// <param name="healthStatus">The health status.</param>
    /// <param name="healthMessage">The health message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated provider implementation.</returns>
    Task<ProviderImplementation> UpdateProviderHealthStatusAsync(
        string implementationId, 
        ProviderHealthStatus healthStatus, 
        string? healthMessage = null, 
        CancellationToken cancellationToken = default);

    #endregion
}
