namespace FluentCMS.Core;

public class EntityHistory<T> : IEntity where T : IEntity
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = default!;
    public string Action { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public T Entity { get; set; } = default!;
    public ApiExecutionContext Context { get; set; } = default!;
}
