using FluentCMS.Providers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentCMS.Providers.Core;

/// <summary>
/// Base class for provider modules that provides common functionality.
/// </summary>
/// <typeparam name="TProvider">The provider implementation type.</typeparam>
/// <typeparam name="TOptions">The options type for the provider.</typeparam>
public abstract class ProviderModuleBase<TProvider, TOptions> : IProviderModule<TProvider, TOptions>
    where TProvider : class, IProvider
    where TOptions : class, new()
{
    /// <summary>
    /// The functional area this provider belongs to (e.g., "Email", "VirtualFile").
    /// </summary>
    public abstract string Area { get; }

    /// <summary>
    /// The display name of this provider for administrative purposes.
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// The type of the provider implementation.
    /// </summary>
    public Type ProviderType => typeof(TProvider);

    /// <summary>
    /// The type of the options class for this provider.
    /// </summary>
    public Type OptionsType => typeof(TOptions);

    /// <summary>
    /// The interface type that this provider implements.
    /// Automatically detects the first interface that extends IProvider.
    /// </summary>
    public virtual Type InterfaceType
    {
        get
        {
            var interfaces = typeof(TProvider).GetInterfaces()
                .Where(i => i != typeof(IProvider) && typeof(IProvider).IsAssignableFrom(i))
                .ToArray();

            if (interfaces.Length == 0)
                throw new InvalidOperationException($"Provider {typeof(TProvider).Name} must implement at least one interface that extends IProvider.");

            // Return the most specific interface (first one that's not IProvider)
            return interfaces.First();
        }
    }

    /// <summary>
    /// Configure additional services required by this provider.
    /// Override this method to register provider-specific services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="providerName">The name of the provider instance as defined in the database.</param>
    public virtual void ConfigureServices(IServiceCollection services, string providerName)
    {
        // Default implementation does nothing
        // Override in derived classes to register additional services
    }

    /// <summary>
    /// Create an instance of the provider with the given service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
    /// <returns>An instance of the provider.</returns>
    public virtual IProvider CreateProvider(IServiceProvider serviceProvider)
    {
        return ActivatorUtilities.CreateInstance<TProvider>(serviceProvider);
    }

    /// <summary>
    /// Validates the provider options. Override to provide custom validation.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>Validation result.</returns>
    protected virtual ValidateOptionsResult ValidateOptions(TOptions options)
    {
        return ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Configures the options for this provider with validation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsName">The name of the options instance.</param>
    /// <param name="configurationJson">The JSON configuration.</param>
    protected virtual void ConfigureOptionsWithValidation(IServiceCollection services, string optionsName, string configurationJson)
    {
        services.AddOptionsWithValidateOnStart<TOptions>(optionsName)
            .Configure(options =>
            {
                if (!string.IsNullOrEmpty(configurationJson))
                {
                    try
                    {
                        var deserializedOptions = System.Text.Json.JsonSerializer.Deserialize<TOptions>(configurationJson);
                        if (deserializedOptions != null)
                        {
                            // Copy properties from deserialized options to the target options
                            var properties = typeof(TOptions).GetProperties().Where(p => p.CanWrite);
                            foreach (var property in properties)
                            {
                                var value = property.GetValue(deserializedOptions);
                                property.SetValue(options, value);
                            }
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Log error but don't throw - use default options
                    }
                }
            })
            .Validate(options => ValidateOptions(options).Succeeded, ValidateOptions(new TOptions()).FailureMessage ?? "Invalid provider options");
    }
}
