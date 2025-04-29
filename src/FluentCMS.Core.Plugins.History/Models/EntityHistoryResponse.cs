namespace FluentCMS.Core.Plugins.History.Models;

public class EntityHistoryResponse
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public object? Entity { get; set; } = default!;
    public ApiExecutionContext Context { get; set; } = default!;
}
