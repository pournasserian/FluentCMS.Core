using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Base interface for provider modules that define how providers are configured and registered.
/// </summary>
public interface IProviderModule
{
    /// <summary>
    /// The functional area this provider belongs to (e.g., "Email", "VirtualFile").
    /// </summary>
    string Area { get; }

    /// <summary>
    /// The display name of this provider for administrative purposes.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The type of the provider implementation.
    /// </summary>
    Type ProviderType { get; }

    /// <summary>
    /// The type of the options class for this provider.
    /// </summary>
    Type OptionsType { get; }

    /// <summary>
    /// The interface type that this provider implements.
    /// </summary>
    Type InterfaceType { get; }

    /// <summary>
    /// Configure additional services required by this provider.
    /// This method is called when the provider is loaded.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="providerName">The name of the provider instance as defined in the database.</param>
    void ConfigureServices(IServiceCollection services, string providerName);

    /// <summary>
    /// Create an instance of the provider with the given service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
    /// <returns>An instance of the provider.</returns>
    IProvider CreateProvider(IServiceProvider serviceProvider);
}


/// <summary>
/// Generic interface for strongly-typed provider modules.
/// </summary>
/// <typeparam name="TProvider">The provider implementation type.</typeparam>
/// <typeparam name="TOptions">The options type for the provider.</typeparam>
public interface IProviderModule<TProvider, TOptions> : IProviderModule
    where TProvider : class, IProvider
    where TOptions : class, new()
{
}
