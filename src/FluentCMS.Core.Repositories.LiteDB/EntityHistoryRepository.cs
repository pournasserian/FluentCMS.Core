namespace FluentCMS.Core.Repositories.LiteDB;

public class EntityHistoryRepository<T> : IEntityHistoryRepository<T> where T : IBaseEntity
{
    private readonly ILiteCollection<EntityHistory<T>> _collection;
    private readonly ILiteDatabase _database;
    private readonly string _collectionName;
    private readonly ILogger<EntityHistoryRepository<T>> _logger;

    public EntityHistoryRepository(ILiteDBContext dbContext, ILogger<EntityHistoryRepository<T>> logger)
    {
        _database = dbContext.Database;
        _collectionName = $"{typeof(T).Name}History";
        _collection = _database.GetCollection<EntityHistory<T>>(_collectionName);

        // Ensure we have indexes for common queries
        _collection.EnsureIndex(x => x.EntityId);
        _collection.EnsureIndex(x => x.EntityType);

        _logger = logger;
    }

    public async Task<IEnumerable<EntityHistory<T>>> GetAll(Guid entityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var history = _collection.Find(h => h.EntityId == entityId)
                .OrderByDescending(h => h.Timestamp)
                .ToList();

            return await Task.FromResult(history);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving history for {EntityType} with ID {EntityId}", typeof(T).Name, entityId);
            throw new RepositoryOperationException(nameof(GetAll), ex);
        }
    }

    public async Task<EntityHistory<T>> Add(T entity, string action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var history = new EntityHistory<T>
            {
                EntityId = entity.Id,
                EntityType = typeof(T).Name,
                Action = action,
                Entity = entity
            };

            _collection.Insert(history);

            _logger.LogInformation("Recorded history for {EntityType} with ID {EntityId}, Action: {Action}", typeof(T).Name, entity.Id, action);

            return await Task.FromResult(history);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error adding history record for {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            throw new RepositoryOperationException(nameof(Add), ex);
        }
    }

    public async Task<IEnumerable<EntityHistory<T>>> GetHistoryByDateRange(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var history = _collection.Find(h => h.Timestamp >= startDate && h.Timestamp <= endDate)
                .OrderByDescending(h => h.Timestamp)
                .ToList();

            return await Task.FromResult(history);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving history by date range for {EntityType}", typeof(T).Name);
            throw new RepositoryOperationException(nameof(GetHistoryByDateRange), ex);
        }
    }

    public async Task<EntityHistory<T>?> GetLatestHistoryForEntity(Guid entityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var history = _collection.Find(h => h.EntityId == entityId)
                .OrderByDescending(h => h.Timestamp)
                .FirstOrDefault();

            return await Task.FromResult(history);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving latest history for {EntityType} with ID {EntityId}", typeof(T).Name, entityId);
            throw new RepositoryOperationException(nameof(GetLatestHistoryForEntity), ex);
        }
    }
}
