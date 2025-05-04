namespace FluentCMS.DataAccess.Abstractions;

public interface IAuditableEntity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    int Version { get; set; }
}

public interface IAuditableEntity : IAuditableEntity<Guid>, IEntity
{
}