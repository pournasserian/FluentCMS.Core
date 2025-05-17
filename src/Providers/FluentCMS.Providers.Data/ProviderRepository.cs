using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Providers.Data;

/// <summary>
/// Repository for provider data operations.
/// </summary>
public class ProviderRepository : IProviderRepository
{
    private readonly ProviderDbContext _dbContext;
    private readonly ILogger<ProviderRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    public ProviderRepository(ProviderDbContext dbContext, ILogger<ProviderRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    #region Provider Types

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderType>> GetProviderTypesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderTypes
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider types");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderType?> GetProviderTypeByIdAsync(string providerTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == providerTypeId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider type by ID: {ProviderTypeId}", providerTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderType?> GetProviderTypeByFullTypeNameAsync(string fullTypeName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.FullTypeName == fullTypeName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider type by full type name: {FullTypeName}", fullTypeName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderType> AddProviderTypeAsync(ProviderType providerType, CancellationToken cancellationToken = default)
    {
        try
        {
            providerType.CreatedAt = DateTimeOffset.UtcNow;
            providerType.UpdatedAt = DateTimeOffset.UtcNow;
            
            await _dbContext.ProviderTypes.AddAsync(providerType, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Added provider type: {ProviderTypeName} ({ProviderTypeId})", 
                providerType.Name, providerType.Id);
            
            return providerType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add provider type: {ProviderTypeName}", providerType.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderType> UpdateProviderTypeAsync(ProviderType providerType, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingProviderType = await _dbContext.ProviderTypes
                .FirstOrDefaultAsync(p => p.Id == providerType.Id, cancellationToken);
            
            if (existingProviderType == null)
            {
                throw new ProviderNotFoundException($"Provider type with ID {providerType.Id} not found");
            }
            
            existingProviderType.Name = providerType.Name;
            existingProviderType.DisplayName = providerType.DisplayName;
            existingProviderType.FullTypeName = providerType.FullTypeName;
            existingProviderType.AssemblyName = providerType.AssemblyName;
            existingProviderType.UpdatedAt = DateTimeOffset.UtcNow;
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Updated provider type: {ProviderTypeName} ({ProviderTypeId})", 
                providerType.Name, providerType.Id);
            
            return existingProviderType;
        }
        catch (ProviderNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provider type: {ProviderTypeId}", providerType.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProviderTypeAsync(string providerTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var providerType = await _dbContext.ProviderTypes
                .FirstOrDefaultAsync(p => p.Id == providerTypeId, cancellationToken);
            
            if (providerType == null)
            {
                return false;
            }
            
            _dbContext.ProviderTypes.Remove(providerType);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Deleted provider type: {ProviderTypeName} ({ProviderTypeId})", 
                providerType.Name, providerTypeId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete provider type: {ProviderTypeId}", providerTypeId);
            throw;
        }
    }

    #endregion

    #region Provider Implementations

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderImplementation>> GetProviderImplementationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderImplementations
                .AsNoTracking()
                .Include(p => p.ProviderType)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider implementations");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderImplementation>> GetProviderImplementationsByTypeAsync(string providerTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderImplementations
                .AsNoTracking()
                .Include(p => p.ProviderType)
                .Where(p => p.ProviderTypeId == providerTypeId)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider implementations for type: {ProviderTypeId}", providerTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderImplementation?> GetProviderImplementationByIdAsync(string implementationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderImplementations
                .AsNoTracking()
                .Include(p => p.ProviderType)
                .FirstOrDefaultAsync(p => p.Id == implementationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider implementation by ID: {ImplementationId}", implementationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderImplementation?> GetProviderImplementationByFullTypeNameAsync(string providerTypeId, string fullTypeName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderImplementations
                .AsNoTracking()
                .Include(p => p.ProviderType)
                .FirstOrDefaultAsync(p => p.ProviderTypeId == providerTypeId && p.FullTypeName == fullTypeName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider implementation by full type name: {FullTypeName} for provider type: {ProviderTypeId}", fullTypeName, providerTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderImplementation?> GetActiveProviderImplementationAsync(string providerTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderImplementations
                .AsNoTracking()
                .Include(p => p.ProviderType)
                .FirstOrDefaultAsync(p => p.ProviderTypeId == providerTypeId && p.IsActive, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active provider implementation for type: {ProviderTypeId}", providerTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderImplementation> AddProviderImplementationAsync(ProviderImplementation implementation, CancellationToken cancellationToken = default)
    {
        try
        {
            implementation.UpdatedAt = DateTimeOffset.UtcNow;
            
            await _dbContext.ProviderImplementations.AddAsync(implementation, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Added provider implementation: {ImplementationName} ({ImplementationId}) for type: {ProviderTypeId}", 
                implementation.Name, implementation.Id, implementation.ProviderTypeId);
            
            return implementation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add provider implementation: {ImplementationName} for type: {ProviderTypeId}", 
                implementation.Name, implementation.ProviderTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderImplementation> UpdateProviderImplementationAsync(ProviderImplementation implementation, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingImplementation = await _dbContext.ProviderImplementations
                .FirstOrDefaultAsync(p => p.Id == implementation.Id, cancellationToken);
            
            if (existingImplementation == null)
            {
                throw new ProviderNotFoundException($"Provider implementation with ID {implementation.Id} not found");
            }
            
            existingImplementation.Name = implementation.Name;
            existingImplementation.Description = implementation.Description;
            existingImplementation.Version = implementation.Version;
            existingImplementation.FullTypeName = implementation.FullTypeName;
            existingImplementation.AssemblyPath = implementation.AssemblyPath;
            existingImplementation.IsInstalled = implementation.IsInstalled;
            existingImplementation.UpdatedAt = DateTimeOffset.UtcNow;
            
            // Only update health-related fields if they are explicitly set
            if (implementation.LastHealthCheckAt.HasValue)
            {
                existingImplementation.HealthStatus = implementation.HealthStatus;
                existingImplementation.HealthMessage = implementation.HealthMessage;
                existingImplementation.LastHealthCheckAt = implementation.LastHealthCheckAt;
            }
            
            // Only update installation time if it is explicitly set
            if (implementation.InstalledAt.HasValue)
            {
                existingImplementation.InstalledAt = implementation.InstalledAt;
            }
            
            // Only update activation time if it is explicitly set
            if (implementation.ActivatedAt.HasValue)
            {
                existingImplementation.ActivatedAt = implementation.ActivatedAt;
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Updated provider implementation: {ImplementationName} ({ImplementationId})", 
                implementation.Name, implementation.Id);
            
            return existingImplementation;
        }
        catch (ProviderNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provider implementation: {ImplementationId}", implementation.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderImplementation> SetActiveProviderImplementationAsync(string providerTypeId, string implementationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the implementation to activate
            var implementation = await _dbContext.ProviderImplementations
                .FirstOrDefaultAsync(p => p.Id == implementationId && p.ProviderTypeId == providerTypeId, cancellationToken);
            
            if (implementation == null)
            {
                throw new ProviderNotFoundException($"Provider implementation with ID {implementationId} not found for type {providerTypeId}");
            }
            
            // Get the currently active implementation, if any
            var activeImplementation = await _dbContext.ProviderImplementations
                .FirstOrDefaultAsync(p => p.ProviderTypeId == providerTypeId && p.IsActive, cancellationToken);
            
            // Start a transaction
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Deactivate the currently active implementation
                if (activeImplementation != null)
                {
                    activeImplementation.IsActive = false;
                    activeImplementation.UpdatedAt = DateTimeOffset.UtcNow;
                }
                
                // Activate the new implementation
                implementation.IsActive = true;
                implementation.ActivatedAt = DateTimeOffset.UtcNow;
                implementation.UpdatedAt = DateTimeOffset.UtcNow;
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogInformation("Set active provider implementation: {ImplementationName} ({ImplementationId}) for type: {ProviderTypeId}", 
                    implementation.Name, implementation.Id, providerTypeId);
                
                return implementation;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to set active provider implementation: {ImplementationId} for type: {ProviderTypeId}", 
                    implementationId, providerTypeId);
                throw;
            }
        }
        catch (ProviderNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set active provider implementation: {ImplementationId} for type: {ProviderTypeId}", 
                implementationId, providerTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProviderImplementationAsync(string implementationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var implementation = await _dbContext.ProviderImplementations
                .FirstOrDefaultAsync(p => p.Id == implementationId, cancellationToken);
            
            if (implementation == null)
            {
                return false;
            }
            
            // Don't allow deletion of active implementations
            if (implementation.IsActive)
            {
                throw new ProviderException("Cannot delete an active provider implementation. Deactivate it first.");
            }
            
            _dbContext.ProviderImplementations.Remove(implementation);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Deleted provider implementation: {ImplementationName} ({ImplementationId})", 
                implementation.Name, implementationId);
            
            return true;
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete provider implementation: {ImplementationId}", implementationId);
            throw;
        }
    }

    #endregion

    #region Provider Configurations

    /// <inheritdoc />
    public async Task<ProviderConfiguration?> GetProviderConfigurationAsync(string implementationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.ProviderConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ImplementationId == implementationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider configuration for implementation: {ImplementationId}", implementationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetTypedProviderConfigurationAsync<T>(string implementationId, CancellationToken cancellationToken = default) where T : class, new()
    {
        try
        {
            var configuration = await GetProviderConfigurationAsync(implementationId, cancellationToken);
            
            if (configuration == null)
            {
                return new T();
            }
            
            try
            {
                return JsonSerializer.Deserialize<T>(configuration.ConfigurationJson, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize configuration for implementation {ImplementationId}", implementationId);
                return new T();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get typed provider configuration for implementation: {ImplementationId}", implementationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderConfiguration> UpdateProviderConfigurationAsync(string implementationId, string configurationJson, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await _dbContext.ProviderConfigurations
                .FirstOrDefaultAsync(p => p.ImplementationId == implementationId, cancellationToken);
            
            if (configuration == null)
            {
                // Create a new configuration
                configuration = new ProviderConfiguration
                {
                    ImplementationId = implementationId,
                    ConfigurationJson = configurationJson,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                
                await _dbContext.ProviderConfigurations.AddAsync(configuration, cancellationToken);
            }
            else
            {
                // Update the existing configuration
                configuration.ConfigurationJson = configurationJson;
                configuration.UpdatedAt = DateTimeOffset.UtcNow;
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Updated provider configuration for implementation: {ImplementationId}", implementationId);
            
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provider configuration for implementation: {ImplementationId}", implementationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProviderConfiguration> UpdateTypedProviderConfigurationAsync<T>(string implementationId, T configuration, CancellationToken cancellationToken = default) where T : class, new()
    {
        try
        {
            var configurationJson = JsonSerializer.Serialize(configuration, _jsonOptions);
            return await UpdateProviderConfigurationAsync(implementationId, configurationJson, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize configuration for implementation {ImplementationId}", implementationId);
            throw new ProviderConfigurationException("Failed to serialize configuration", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update typed provider configuration for implementation: {ImplementationId}", implementationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProviderConfigurationAsync(string implementationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await _dbContext.ProviderConfigurations
                .FirstOrDefaultAsync(p => p.ImplementationId == implementationId, cancellationToken);
            
            if (configuration == null)
            {
                return false;
            }
            
            _dbContext.ProviderConfigurations.Remove(configuration);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Deleted provider configuration for implementation: {ImplementationId}", implementationId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete provider configuration for implementation: {ImplementationId}", implementationId);
            throw;
        }
    }

    #endregion

    #region Health Status

    /// <inheritdoc />
    public async Task<ProviderImplementation> UpdateProviderHealthStatusAsync(string implementationId, ProviderHealthStatus healthStatus, string? healthMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var implementation = await _dbContext.ProviderImplementations
                .FirstOrDefaultAsync(p => p.Id == implementationId, cancellationToken);
            
            if (implementation == null)
            {
                throw new ProviderNotFoundException($"Provider implementation with ID {implementationId} not found");
            }
            
            implementation.HealthStatus = healthStatus;
            implementation.HealthMessage = healthMessage;
            implementation.LastHealthCheckAt = DateTimeOffset.UtcNow;
            implementation.UpdatedAt = DateTimeOffset.UtcNow;
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Updated health status for provider implementation: {ImplementationName} ({ImplementationId}) to {HealthStatus}", 
                implementation.Name, implementation.Id, healthStatus);
            
            return implementation;
        }
        catch (ProviderNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update health status for provider implementation: {ImplementationId}", implementationId);
            throw;
        }
    }

    #endregion
}
