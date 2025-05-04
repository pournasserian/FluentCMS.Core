namespace FluentCMS.DataAccess.Abstractions;

public abstract class AuditableEntity<TKey>: Entity<TKey>, IAuditableEntity<TKey> where TKey : IEquatable<TKey>
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int Version { get; set; }
}


public abstract class AuditableEntity : AuditableEntity<Guid>, IAuditableEntity
{
}