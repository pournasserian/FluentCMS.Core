namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Health status of a provider.
/// </summary>
public enum ProviderHealthStatus
{
    /// <summary>
    /// Provider is healthy and functioning properly.
    /// </summary>
    Healthy,

    /// <summary>
    /// Provider is functioning but with degraded performance or limited functionality.
    /// </summary>
    Degraded,

    /// <summary>
    /// Provider is not functioning and cannot be used.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Interface for provider health monitoring.
/// Implement this interface to report provider health status and metrics.
/// </summary>
public interface IProviderHealth
{
    /// <summary>
    /// Gets the current health status of the provider.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns the health status and a status message.</returns>
    Task<(ProviderHealthStatus Status, string Message)> GetStatus(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the metrics for the provider.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns a dictionary of metric names and values.</returns>
    Task<Dictionary<string, object>> GetMetrics(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a self-test of the provider to verify functionality.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that returns a boolean indicating success or failure and a message.</returns>
    Task<(bool Success, string Message)> PerformSelfTest(CancellationToken cancellationToken = default);
}
