namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Abstract base class for provider implementations.
/// This class provides default implementations for common provider functionality.
/// </summary>
public abstract class ProviderBase : IProvider, IProviderLifecycle, IProviderHealth
{
    /// <summary>
    /// Gets the unique identifier for this provider implementation.
    /// </summary>
    public string Id { get; protected set; } = null!;

    /// <summary>
    /// Gets the display name of this provider implementation.
    /// </summary>
    public string Name { get; protected set; } = null!;

    /// <summary>
    /// Gets the description of this provider implementation.
    /// </summary>
    public string? Description { get; protected set; }

    /// <summary>
    /// Gets the version of this provider implementation.
    /// </summary>
    public string Version { get; protected set; } = "1.0.0";

    /// <summary>
    /// Called when the provider is first initialized.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the initialization operation.</returns>
    public virtual Task Initialize(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the provider is activated and becomes the active implementation.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the activation operation.</returns>
    public virtual Task Activate(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the provider is deactivated and is no longer the active implementation.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the deactivation operation.</returns>
    public virtual Task Deactivate(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the provider is being uninstalled.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the uninstallation operation.</returns>
    public virtual Task Uninstall(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current health status of the provider.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns the health status and a status message.</returns>
    public virtual Task<(ProviderHealthStatus Status, string Message)> GetStatus(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((ProviderHealthStatus.Healthy, "Provider is healthy"));
    }

    /// <summary>
    /// Gets the metrics for the provider.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns a dictionary of metric names and values.</returns>
    public virtual Task<Dictionary<string, object>> GetMetrics(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Dictionary<string, object>());
    }

    /// <summary>
    /// Performs a self-test of the provider to verify functionality.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns a boolean indicating success or failure and a message.</returns>
    public virtual Task<(bool Success, string Message)> PerformSelfTest(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((true, "Self-test passed"));
    }
}
