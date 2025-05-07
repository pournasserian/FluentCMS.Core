namespace FluentCMS.Plugins.AuditTrailManager.Models;

public class AuditTrail : Entity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    private string _entityJson = default!;

    [NotMapped]
    public object Entity
    {
        get => JsonSerializer.Deserialize<object>(_entityJson, new JsonSerializerOptions { })!;
        set => _entityJson = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
    }

    public string EntityJson
    {
        get => _entityJson;
        set => _entityJson = value;
    }

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
