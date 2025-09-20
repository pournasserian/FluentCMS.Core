using System;
using System.Data;

namespace FluentCMS.DataSeeding.Models;

/// <summary>
/// Provides database connection and service access for seeding operations.
/// This abstract class must be implemented by database-specific providers.
/// </summary>
public abstract class SeedingContext : IDisposable
{
    private bool _disposed = false;

    /// <summary>
    /// Gets a database connection for the current seeding operation.
    /// Each call may return a new connection instance - callers should dispose appropriately.
    /// For databases that don't use IDbConnection (like MongoDB), this may return null.
    /// </summary>
    /// <returns>A database connection ready for use, or null for non-SQL databases</returns>
    public abstract IDbConnection? GetConnection();

    /// <summary>
    /// Gets a required service from the dependency injection container.
    /// This allows seeders to access application services like repositories or configuration.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The requested service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not found</exception>
    public abstract T GetRequiredService<T>() where T : notnull;

    /// <summary>
    /// Gets a service from the dependency injection container, returning null if not found.
    /// This is useful for optional dependencies.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The requested service instance or null if not found</returns>
    public abstract T? GetService<T>() where T : class;

    /// <summary>
    /// Protected implementation of dispose pattern for derived classes.
    /// Override this method to dispose database-specific resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Base class has no resources to dispose
            // Derived classes should override to dispose their resources
        }
        _disposed = true;
    }

    /// <summary>
    /// Disposes the seeding context and any held resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
