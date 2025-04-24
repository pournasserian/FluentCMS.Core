namespace FluentCMS.Core.Repositories.LiteDB;

public class LiteDBRepository<T> : IBaseEntityRepository<T> where T : IBaseEntity
{
    private readonly ILiteCollection<T> _collection;
    private readonly ILiteDatabase _database;
    private readonly string _entityName;
    private readonly ILogger<LiteDBRepository<T>> _logger;
    private readonly IEventPublisher _eventPublisher;

    public LiteDBRepository(ILiteDBContext dbContext, ILogger<LiteDBRepository<T>> logger, IEventPublisher eventPublisher)
    {
        _database = dbContext.Database;
        _entityName = typeof(T).Name;
        _collection = _database.GetCollection<T>(_entityName);

        // Ensure we have an index on Id field
        _collection.EnsureIndex(x => x.Id);
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task<T> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // LiteDB is synchronous, but we'll wrap in Task to comply with interface
            var entity = _collection.FindById(id);

            if (entity == null)
            {
                _logger.LogCritical("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found", nameof(GetById), _entityName, id);
                throw new ApplicationException($"Entity with ID {id} not found in {_entityName} collection.");
            }

            return await Task.FromResult(entity);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: Error while retrieving {EntityType} with ID {EntityId}", nameof(GetById), _entityName, id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Use Query API for consistency with other methods
            var entities = _collection.Query().ToList();
            return await Task.FromResult(entities);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: Error while retrieving all {EntityType} entities", nameof(GetAll), _entityName);
            throw;
        }
    }

    public async Task<QueryResult<T>> Query(Expression<Func<T, bool>>? filter = default, PaginationOptions? paginationOptions = default, IList<SortOption<T>>? sortOptions = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new QueryOptions<T>
        {
            Filter = filter,
            Pagination = paginationOptions,
            Sorting = sortOptions
        };

        return await Query(options, cancellationToken);
    }

    public async Task<QueryResult<T>> Query(QueryOptions<T> options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Build the LiteDB query
            ILiteQueryable<T> query = _collection.Query();

            // Apply filter if provided
            if (options.Filter != null)
                query = query.Where(options.Filter);

            // Calculate total count before applying pagination and sorting
            int totalCount = query.Count();

            // Apply sorting if provided
            if (options.Sorting != null && options.Sorting.Any())
            {
                // LiteDB only supports sorting on one field at a time with their Query API
                // We'll use the first sort option (most important one)
                var firstSort = options.Sorting.First();

                // Extract the property name from the expression
                string propertyName = ExpressionHelpers.ExtractPropertyNameFromExpression(firstSort.KeySelector);

                // Apply the sort
                query = firstSort.Direction == SortDirection.Ascending
                    ? query.OrderBy(propertyName)
                    : query.OrderByDescending(propertyName);
                // Note: Additional sort options are not supported directly by LiteDB's query API
                // For multiple sorting fields, we would need to use a custom approach
            }

            // Execute query and return results
            var result = new QueryResult<T>
            {
                TotalCount = totalCount
            };

            // Apply pagination if provided
            if (options.Pagination == null)
                result.Items = query.ToEnumerable();
            else
                result.Items = query.Skip(options.Pagination.Skip).Limit(options.Pagination.PageSize).ToEnumerable();

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: Error while querying {EntityType} entities", nameof(Query), _entityName);
            throw;
        }
    }

    public async Task<int> Count(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Use LiteDB's Query API for counting to fully leverage database capabilities
            if (filter == null)
            {
                return await Task.FromResult(_collection.Count());
            }
            else
            {
                return await Task.FromResult(_collection.Query().Where(filter).Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: Error while counting {EntityType} entities", nameof(Count), _entityName);
            throw;
        }
    }

    public async Task<T> Add(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            var inserted = _collection.Insert(entity);
            if (inserted == null)
            {
                var message = "Critical error in {MethodName}: Failed to add {EntityType} with ID {EntityId}";
                _logger.LogCritical(message, nameof(Add), _entityName, entity.Id);
                throw new ApplicationException(message);
            }

            // Publish event after successful addition
            await _eventPublisher.Publish(entity, $"{typeof(T).Name}.Created", cancellationToken);

            return entity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Add), _entityName, entity.Id);
            throw;
        }
    }

    public async Task<T> Update(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Verify entity exists before update
            if (!_collection.Exists(e => e.Id == entity.Id))
            {
                var message = "Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for update";
                _logger.LogCritical(message, nameof(Update), _entityName, entity.Id);
                throw new ApplicationException("Entity not found!");
            }

            // Get the original entity for history
            var originalEntity = _collection.FindById(entity.Id);

            // Publish event with original entity before update
            await _eventPublisher.Publish(originalEntity, $"{typeof(T).Name}.Updated", cancellationToken);

            var updated = _collection.Update(entity);
            if (!updated)
            {
                var message = "Critical error in {MethodName}: Failed to update {EntityType} with ID {EntityId}";
                _logger.LogCritical(message, nameof(Update), _entityName, entity.Id);
                throw new ApplicationException(message);
            }

            return entity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Update), _entityName, entity.Id);
            throw;
        }
    }

    public async Task Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Verify entity exists before deletion
            if (!_collection.Exists(e => e.Id == id))
            {
                var message = "Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for removal";
                _logger.LogCritical(message, nameof(Remove), _entityName, id);
                throw new ApplicationException(message);
            }

            // Get the entity for history
            var entity = _collection.FindById(id);

            // Publish event before deletion
            await _eventPublisher.Publish(entity, $"{typeof(T).Name}.Deleted", cancellationToken);

            var deleted = _collection.Delete(id);
            if (!deleted)
            {
                var message = "Critical error in {MethodName}: Failed to remove {EntityType} with ID {EntityId}";
                _logger.LogCritical(message, nameof(Remove), _entityName, id);
                throw new ApplicationException(message);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Remove), _entityName, id);
            throw;
        }
    }
}
