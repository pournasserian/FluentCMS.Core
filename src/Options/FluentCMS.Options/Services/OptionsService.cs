using FluentCMS.Options.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FluentCMS.Options.Services;

public interface IOptionsService
{
    Task<T?> Get<T>(string alias, CancellationToken cancellationToken = default) where T : class, new();
    Task Update<T>(string alias, T value, CancellationToken cancellationToken = default) where T : class, new();
    Task<string> Get(string alias, string typeName, CancellationToken cancellationToken = default);
    Task Bind<T>(string alias, T options, CancellationToken cancellationToken = default) where T : class, new();
    Task<bool> Exists(string alias, string typeName, CancellationToken cancellationToken = default);
}

internal class OptionsService(IOptionsRepository optionsRepository, ILogger<OptionsService> logger) : IOptionsService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public async Task Bind<T>(string alias, T options, CancellationToken cancellationToken = default) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(alias, nameof(alias));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        try
        {
            var jsonOptions = await Get(alias, options.GetType().FullName!, cancellationToken);
            if (!string.IsNullOrEmpty(jsonOptions))
                UpdateFromJson(options, jsonOptions);
        }
        catch (KeyNotFoundException)
        {
            // No existing options found, use default values
            logger.LogDebug("No existing options found for alias: {Alias}, using default values", alias);
        }
    }

    public async Task<T?> Get<T>(string alias, CancellationToken cancellationToken = default) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(alias, nameof(alias));

        try
        {
            var options = await optionsRepository.GetByAliasType(alias, typeof(T).FullName!, cancellationToken);

            if (options == null || string.IsNullOrEmpty(options.Value))
            {
                logger.LogWarning("No options found for alias: {Alias}", alias);
                return default;
            }

            return JsonSerializer.Deserialize<T>(options.Value, _jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize options for alias: {Alias}", alias);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving options for alias: {Alias}", alias);
            throw;
        }
    }

    public async Task<string> Get(string alias, string typeName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alias, nameof(alias));

        try
        {
            var options = await optionsRepository.GetByAliasType(alias, typeName, cancellationToken);

            if (options == null || string.IsNullOrEmpty(options.Value))
            {
                var ex = new KeyNotFoundException($"No options found for alias: {alias}");
                logger.LogError(ex, "No options found for alias: {Alias}", alias);
                throw ex;
            }

            return options.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving options for alias: {Alias}", alias);
            throw;
        }
    }

    public async Task Update<T>(string alias, T value, CancellationToken cancellationToken = default) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(alias, nameof(alias));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        try
        {
            var options = await optionsRepository.GetByAliasType(alias, value.GetType().FullName!, cancellationToken);
            if (options == null)
            {
                options = new OptionsDbModel
                {
                    Alias = alias,
                    Type = value.GetType().FullName!,
                    Value = JsonSerializer.Serialize(value, _jsonSerializerOptions)
                };
                await optionsRepository.Add(options, cancellationToken);
                logger.LogInformation("Added new options for alias: {Alias}", alias);
            }
            else
            {
                options.Value = JsonSerializer.Serialize(value, _jsonSerializerOptions);
                await optionsRepository.Update(options, cancellationToken);
                logger.LogInformation("Updated options for alias: {Alias}", alias);
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to serialize options for alias: {Alias}", alias);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating options for alias: {Alias}", alias);
            throw;
        }
    }

    internal void UpdateFromJson<T>(T target, string json)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonSerializerOptions);
            if (dict == null)
                return;

            var type = typeof(T);
            foreach (var kvp in dict)
            {
                var prop = type.GetProperty(kvp.Key);
                if (prop != null && prop.CanWrite)
                {
                    var value = kvp.Value.Deserialize(prop.PropertyType, _jsonSerializerOptions);
                    prop.SetValue(target, value);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating object of type {Type} from JSON.", typeof(T).FullName);
            throw;
        }
    }

    public async Task<bool> Exists(string alias, string typeName, CancellationToken cancellationToken = default)
    {
        return await optionsRepository.GetByAliasType(alias, typeName, cancellationToken) != null;
    }
}
