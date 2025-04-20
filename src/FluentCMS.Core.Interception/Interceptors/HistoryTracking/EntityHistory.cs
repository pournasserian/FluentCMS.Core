using FluentCMS.Core.Repositories.Abstractions;

namespace FluentCMS.Core.Interception.Interceptors.HistoryTracking;

/// <summary>
/// Represents a historical record of an entity at a specific point in time.
/// </summary>
/// <typeparam name="T">The type of entity being tracked.</typeparam>
public class EntityHistory<T> where T : IBaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this history record.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the entity this history record relates to.
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the entity type.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the action that was performed on the entity.
    /// </summary>
    /// <remarks>Possible values are "Create", "Update", or "Delete".</remarks>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the timestamp when this history record was created.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets a snapshot of the entity at the time this history record was created.
    /// </summary>
    public T Entity { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the username of the user who performed the action.
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
