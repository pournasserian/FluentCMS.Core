using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.Core.Repositories.LiteDB;
using LiteDB;

namespace FluentCMS.Core.Interception.Interceptors.HistoryTracking.LiteDB;

/// <summary>
/// Implementation of IHistoryRecorder that uses LiteDB to store entity history.
/// </summary>
public class LiteDBHistoryRecorder : IHistoryRecorder
{
    private readonly ILiteDBContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteDBHistoryRecorder"/> class.
    /// </summary>
    /// <param name="dbContext">The LiteDB context to use.</param>
    public LiteDBHistoryRecorder(ILiteDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task RecordHistory<T>(T entity, string action, string username) where T : IBaseEntity
    {
        var history = new EntityHistory<T>
        {
            Id = Guid.NewGuid(),
            EntityId = entity.Id,
            EntityType = typeof(T).Name,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Entity = entity,
            Username = username
        };

        var collectionName = $"{typeof(T).Name}History";
        var collection = _dbContext.Database.GetCollection<EntityHistory<T>>(collectionName);

        // Ensure we have an index on EntityId for faster lookups
        collection.EnsureIndex(x => x.EntityId);
        // Also index on timestamp for point-in-time lookups
        collection.EnsureIndex(x => x.Timestamp);

        collection.Insert(history);

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EntityHistory<T>>> GetHistoryForEntity<T>(Guid entityId) where T : IBaseEntity
    {
        var collectionName = $"{typeof(T).Name}History";
        var collection = _dbContext.Database.GetCollection<EntityHistory<T>>(collectionName);

        var history = collection
            .Find(h => h.EntityId == entityId)
            .OrderByDescending(h => h.Timestamp)
            .ToList();

        return await Task.FromResult(history);
    }

    /// <inheritdoc />
    public async Task<T?> GetEntityAtPointInTime<T>(Guid entityId, DateTime pointInTime) where T : IBaseEntity
    {
        var collectionName = $"{typeof(T).Name}History";
        var collection = _dbContext.Database.GetCollection<EntityHistory<T>>(collectionName);

        // Find the most recent history record that is older than or at the specified point in time
        var historyRecord = collection
            .Find(h => h.EntityId == entityId && h.Timestamp <= pointInTime)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefault();

        if (historyRecord != null)
        {
            // Check if the entity was deleted after this point in time but before the next history record
            var wasDeletedAfter = collection
                .Find(h => h.EntityId == entityId && h.Timestamp > pointInTime && h.Action == "Delete")
                .Any();

            if (historyRecord.Action == "Delete")
            {
                // If this record is a delete, the entity didn't exist at this point
                return await Task.FromResult<T?>(default);
            }

            return await Task.FromResult(historyRecord.Entity);
        }

        // No history found, check if the entity currently exists
        var entityCollection = _dbContext.Database.GetCollection<T>(typeof(T).Name);
        var currentEntity = entityCollection.FindById(entityId);

        // If entity exists and was created before the specified point in time, return it
        if (currentEntity != null)
        {
            // Since we have no history, we can't determine when it was created
            // We'll assume it exists if there's no Delete record in history
            var wasDeleted = collection
                .Find(h => h.EntityId == entityId && h.Action == "Delete")
                .Any();

            if (!wasDeleted)
            {
                return await Task.FromResult(currentEntity);
            }
        }

        return await Task.FromResult<T?>(default);
    }
}
