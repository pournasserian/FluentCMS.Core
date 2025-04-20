namespace FluentCMS.Core.Repositories.Abstractions;

/// <summary>
/// Represents the base interface for all entities in the system.
/// All entities must have a GUID identifier.
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    Guid Id { get; set; }
}
