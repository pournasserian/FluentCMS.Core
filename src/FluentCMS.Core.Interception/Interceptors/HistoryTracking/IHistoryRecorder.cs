using FluentCMS.Core.Repositories.Abstractions;

namespace FluentCMS.Core.Interception.Interceptors.HistoryTracking;

/// <summary>
/// Defines methods for recording entity history.
/// </summary>
public interface IHistoryRecorder
{
    /// <summary>
    /// Records a history entry for an entity.
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
    /// <param name="entity">The entity to record history for.</param>
    /// <param name="action">The action performed on the entity ("Create", "Update", or "Delete").</param>
    /// <param name="username">The username of the user who performed the action.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RecordHistory<T>(T entity, string action, string username) where T : IBaseEntity;
    
    /// <summary>
    /// Gets the history entries for an entity.
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
    /// <param name="entityId">The identifier of the entity.</param>
    /// <returns>A collection of history entries for the entity.</returns>
    Task<IEnumerable<EntityHistory<T>>> GetHistoryForEntity<T>(Guid entityId) where T : IBaseEntity;
    
    /// <summary>
    /// Gets an entity as it existed at a specific point in time.
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
    /// <param name="entityId">The identifier of the entity.</param>
    /// <param name="pointInTime">The point in time to retrieve the entity at.</param>
    /// <returns>The entity as it existed at the specified point in time, or null if it did not exist.</returns>
    Task<T?> GetEntityAtPointInTime<T>(Guid entityId, DateTime pointInTime) where T : IBaseEntity;
}
