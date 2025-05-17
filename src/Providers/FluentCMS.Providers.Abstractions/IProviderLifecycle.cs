namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Interface for provider lifecycle management.
/// Implement this interface to handle initialization, activation, deactivation, and uninstallation events.
/// </summary>
public interface IProviderLifecycle
{
    /// <summary>
    /// Called when the provider is first initialized.
    /// This is a good place to perform one-time setup operations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the initialization operation.</returns>
    Task Initialize(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the provider is activated and becomes the active implementation.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the activation operation.</returns>
    Task Activate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the provider is deactivated and is no longer the active implementation.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the deactivation operation.</returns>
    Task Deactivate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the provider is being uninstalled.
    /// This is a good place to clean up any resources created by the provider.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the uninstallation operation.</returns>
    Task Uninstall(CancellationToken cancellationToken = default);
}
