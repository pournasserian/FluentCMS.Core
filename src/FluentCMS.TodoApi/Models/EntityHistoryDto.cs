using FluentCMS.Core;
using FluentCMS.Core.Repositories.History;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentCMS.TodoApi.Models;

public class EntityHistoryDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    [JsonIgnore]
    public object? Entity { get; set; }

    public JsonDocument? EntityData => Entity != null
        ? JsonDocument.Parse(JsonSerializer.Serialize(Entity))
        : null;

    public static EntityHistoryDto FromEntityHistory<T>(EntityHistory<T> history) where T : IBaseEntity
    {
        return new EntityHistoryDto
        {
            Id = history.Id,
            EntityId = history.EntityId,
            EntityType = history.EntityType,
            Action = history.Action,
            Timestamp = history.Timestamp,
            Entity = history.Entity
        };
    }
}
