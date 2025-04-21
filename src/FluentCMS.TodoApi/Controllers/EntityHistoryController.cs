using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.TodoApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace FluentCMS.TodoApi.Controllers;

[ApiController]
[Route("api/history")]
public class EntityHistoryController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EntityHistoryController> _logger;

    public EntityHistoryController(
        IServiceProvider serviceProvider,
        ILogger<EntityHistoryController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [HttpGet("{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<EntityHistoryDto>>> GetHistory(string entityType, Guid entityId)
    {
        try
        {
            // Get the entity type from the string name
            Type? type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name.Equals(entityType, StringComparison.OrdinalIgnoreCase));

            if (type == null)
            {
                return NotFound($"Entity type '{entityType}' not found");
            }

            // Get the generic repository type
            Type historyRepoType = typeof(IEntityHistoryRepository<>).MakeGenericType(type);

            // Resolve the repository from the service provider
            var historyRepo = _serviceProvider.GetService(historyRepoType);

            if (historyRepo == null)
            {
                return NotFound($"History repository for '{entityType}' not registered");
            }

            // Use reflection to call the GetAll method
            var method = historyRepoType.GetMethod("GetAll");
            var task = method?.Invoke(historyRepo, new object[] { entityId, default(CancellationToken) });

            if (task == null)
            {
                return StatusCode(500, "Failed to execute history query");
            }

            // Wait for the task to complete
            await (Task)task;

            // Get the result property from the Task
            var resultProperty = task.GetType().GetProperty("Result");
            var historyItems = resultProperty?.GetValue(task) as IEnumerable;

            if (historyItems == null)
            {
                return new List<EntityHistoryDto>();
            }

            // Convert each history item to EntityHistoryDto
            var result = new List<EntityHistoryDto>();
            foreach (var item in historyItems)
            {
                // Use reflection to get the properties needed for EntityHistoryDto
                var idProp = item.GetType().GetProperty("Id");
                var entityIdProp = item.GetType().GetProperty("EntityId");
                var entityTypeProp = item.GetType().GetProperty("EntityType");
                var actionProp = item.GetType().GetProperty("Action");
                var timestampProp = item.GetType().GetProperty("Timestamp");
                var entityProp = item.GetType().GetProperty("Entity");

                var dto = new EntityHistoryDto
                {
                    Id = (Guid)(idProp?.GetValue(item) ?? Guid.Empty),
                    EntityId = (Guid)(entityIdProp?.GetValue(item) ?? Guid.Empty),
                    EntityType = (string)(entityTypeProp?.GetValue(item) ?? string.Empty),
                    Action = (string)(actionProp?.GetValue(item) ?? string.Empty),
                    Timestamp = (DateTime)(timestampProp?.GetValue(item) ?? DateTime.MinValue),
                    Entity = entityProp?.GetValue(item)
                };

                result.Add(dto);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for {EntityType} with ID {EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while retrieving entity history");
        }
    }

    [HttpGet("date-range/{entityType}")]
    public async Task<ActionResult<IEnumerable<EntityHistoryDto>>> GetHistoryByDateRange(
        string entityType,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            // Get the entity type from the string name
            Type? type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name.Equals(entityType, StringComparison.OrdinalIgnoreCase));

            if (type == null)
            {
                return NotFound($"Entity type '{entityType}' not found");
            }

            // Get the generic repository type
            Type historyRepoType = typeof(IEntityHistoryRepository<>).MakeGenericType(type);

            // Resolve the repository from the service provider
            var historyRepo = _serviceProvider.GetService(historyRepoType);

            if (historyRepo == null)
            {
                return NotFound($"History repository for '{entityType}' not registered");
            }

            // Use reflection to call the GetHistoryByDateRange method
            var method = historyRepoType.GetMethod("GetHistoryByDateRange");
            var task = method?.Invoke(historyRepo, new object[] { startDate, endDate, default(CancellationToken) });

            if (task == null)
            {
                return StatusCode(500, "Failed to execute history query");
            }

            // Wait for the task to complete
            await (Task)task;

            // Get the result property from the Task
            var resultProperty = task.GetType().GetProperty("Result");
            var historyItems = resultProperty?.GetValue(task) as IEnumerable;

            if (historyItems == null)
            {
                return new List<EntityHistoryDto>();
            }

            // Convert each history item to EntityHistoryDto
            var result = new List<EntityHistoryDto>();
            foreach (var item in historyItems)
            {
                // Use reflection to get the properties needed for EntityHistoryDto
                var idProp = item.GetType().GetProperty("Id");
                var entityIdProp = item.GetType().GetProperty("EntityId");
                var entityTypeProp = item.GetType().GetProperty("EntityType");
                var actionProp = item.GetType().GetProperty("Action");
                var timestampProp = item.GetType().GetProperty("Timestamp");
                var entityProp = item.GetType().GetProperty("Entity");

                var dto = new EntityHistoryDto
                {
                    Id = (Guid)(idProp?.GetValue(item) ?? Guid.Empty),
                    EntityId = (Guid)(entityIdProp?.GetValue(item) ?? Guid.Empty),
                    EntityType = (string)(entityTypeProp?.GetValue(item) ?? string.Empty),
                    Action = (string)(actionProp?.GetValue(item) ?? string.Empty),
                    Timestamp = (DateTime)(timestampProp?.GetValue(item) ?? DateTime.MinValue),
                    Entity = entityProp?.GetValue(item)
                };

                result.Add(dto);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history by date range for {EntityType}", entityType);
            return StatusCode(500, "An error occurred while retrieving entity history");
        }
    }

    [HttpGet("latest/{entityType}/{entityId}")]
    public async Task<ActionResult<EntityHistoryDto>> GetLatestHistory(string entityType, Guid entityId)
    {
        try
        {
            // Get the entity type from the string name
            Type? type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name.Equals(entityType, StringComparison.OrdinalIgnoreCase));

            if (type == null)
            {
                return NotFound($"Entity type '{entityType}' not found");
            }

            // Get the generic repository type
            Type historyRepoType = typeof(IEntityHistoryRepository<>).MakeGenericType(type);

            // Resolve the repository from the service provider
            var historyRepo = _serviceProvider.GetService(historyRepoType);

            if (historyRepo == null)
            {
                return NotFound($"History repository for '{entityType}' not registered");
            }

            // Use reflection to call the GetLatestHistoryForEntity method
            var method = historyRepoType.GetMethod("GetLatestHistoryForEntity");
            var task = method?.Invoke(historyRepo, new object[] { entityId, default(CancellationToken) });

            if (task == null)
            {
                return StatusCode(500, "Failed to execute history query");
            }

            // Wait for the task to complete
            await (Task)task;

            // Get the result property from the Task
            var resultProperty = task.GetType().GetProperty("Result");
            var historyItem = resultProperty?.GetValue(task);

            if (historyItem == null)
            {
                return NotFound($"No history found for {entityType} with ID {entityId}");
            }

            // Use reflection to get the properties needed for EntityHistoryDto
            var idProp = historyItem.GetType().GetProperty("Id");
            var entityIdProp = historyItem.GetType().GetProperty("EntityId");
            var entityTypeProp = historyItem.GetType().GetProperty("EntityType");
            var actionProp = historyItem.GetType().GetProperty("Action");
            var timestampProp = historyItem.GetType().GetProperty("Timestamp");
            var entityProp = historyItem.GetType().GetProperty("Entity");

            var dto = new EntityHistoryDto
            {
                Id = (Guid)(idProp?.GetValue(historyItem) ?? Guid.Empty),
                EntityId = (Guid)(entityIdProp?.GetValue(historyItem) ?? Guid.Empty),
                EntityType = (string)(entityTypeProp?.GetValue(historyItem) ?? string.Empty),
                Action = (string)(actionProp?.GetValue(historyItem) ?? string.Empty),
                Timestamp = (DateTime)(timestampProp?.GetValue(historyItem) ?? DateTime.MinValue),
                Entity = entityProp?.GetValue(historyItem)
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest history for {EntityType} with ID {EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while retrieving entity history");
        }
    }
}
