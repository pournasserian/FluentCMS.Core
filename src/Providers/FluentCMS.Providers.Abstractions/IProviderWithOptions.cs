namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Interface for providers that have configuration options.
/// Implement this interface to define and validate configuration options for a provider.
/// </summary>
/// <typeparam name="TOptions">The type of options for the provider.</typeparam>
public interface IProviderWithOptions<TOptions> where TOptions : class, new()
{
    /// <summary>
    /// Gets the current configuration options for the provider.
    /// </summary>
    /// <returns>The current configuration options.</returns>
    TOptions GetOptions();

    /// <summary>
    /// Validates the provided configuration options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that returns a validation result indicating success or failure,
    /// along with any validation error messages.
    /// </returns>
    Task<(bool IsValid, string[] Errors)> ValidateOptions(
        TOptions options, 
        CancellationToken cancellationToken = default);
}
