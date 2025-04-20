namespace FluentCMS.Core.Repositories.Abstractions;

public class EntityHistory<T> : IBaseEntity where T : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = default!;
    public string Action { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public T Entity { get; set; } = default!;
}
