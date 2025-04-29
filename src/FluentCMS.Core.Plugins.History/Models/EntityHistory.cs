namespace FluentCMS.Core.Plugins.History.Models;

public class EntityHistory : Entity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public object Entity { get; set; } = default!;

    // IApplicationExecutionContext fields
    public bool IsAuthenticated { get; set; }
    public string Language { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public string TraceId { get; set; } = default!;
    public string UniqueId { get; set; } = default!;
    public Guid? UserId { get; set; }
    public string UserIp { get; set; } = default!;
    public string Username { get; set; } = default!;
}
