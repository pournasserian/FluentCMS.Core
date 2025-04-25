namespace FluentCMS.Core.Repositories.LiteDB;

public class LiteDBRepository<T> : IEntityRepository<T> where T : IEntity
{
    private readonly ILiteCollection<T> _collection;
    private readonly ILiteDatabase _database;
    private readonly string _entityName;
    private readonly ILogger<LiteDBRepository<T>> _logger;
    private readonly IEventPublisher _eventPublisher;
    private readonly ApiExecutionContext _executionContext;

    public LiteDBRepository(ILiteDBContext dbContext, ILogger<LiteDBRepository<T>> logger, IEventPublisher eventPublisher, ApiExecutionContext executionContext)
    {
        _database = dbContext.Database;
        _entityName = typeof(T).Name;
        _collection = _database.GetCollection<T>(_entityName);

        // Ensure we have an index on Id field
        _collection.EnsureIndex(x => x.Id);
        _logger = logger;
        _eventPublisher = eventPublisher;
        _executionContext = executionContext;
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
                throw new EntityNotFoundException(id, _entityName);
            }

            return await Task.FromResult(entity);
        }
        catch (EntityNotFoundException)
        {
            // Re-throw EntityNotFoundException without wrapping
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: Error while retrieving {EntityType} with ID {EntityId}", nameof(GetById), _entityName, id);
            throw new RepositoryOperationException(nameof(GetById), ex);
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
            throw new RepositoryOperationException(nameof(GetAll), ex);
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
            throw new RepositoryOperationException(nameof(Query), ex);
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
            throw new RepositoryOperationException(nameof(Count), ex);
        }
    }

    public async Task<T> Add(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            // check if T is IAuditableEntity
            if (entity is IAuditableEntity auditableEntity)
            {
                auditableEntity.CreatedAt = DateTime.UtcNow;
                auditableEntity.CreatedBy = _executionContext.Username;
                auditableEntity.ModifiedBy = null;
                auditableEntity.ModifiedAt = null;
            }

            var inserted = _collection.Insert(entity);
            if (inserted == null)
            {
                _logger.LogCritical("Critical error in {MethodName}: Failed to add {EntityType} with ID {EntityId}", nameof(Add), _entityName, entity.Id);
                throw new RepositoryOperationException(nameof(Add), $"Failed to add entity with ID {entity.Id}");
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
            // Get the original entity for history
            var originalEntity = _collection.FindById(entity.Id);

            // Verify entity exists before update
            if (originalEntity is null)
            {
                _logger.LogCritical("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for update", nameof(Update), _entityName, entity.Id);
                throw new EntityNotFoundException(entity.Id, _entityName);
            }

            // check if T is IAuditableEntity
            if (entity is IAuditableEntity auditableEntity)
            {
                auditableEntity.CreatedAt = ((IAuditableEntity)originalEntity).CreatedAt;
                auditableEntity.CreatedBy = ((IAuditableEntity)originalEntity).CreatedBy;
                auditableEntity.ModifiedBy = _executionContext.Username;
                auditableEntity.ModifiedAt = DateTime.UtcNow;
            }

            var updated = _collection.Update(entity);
            if (!updated)
            {
                _logger.LogCritical("Critical error in {MethodName}: Failed to update {EntityType} with ID {EntityId}", nameof(Update), _entityName, entity.Id);
                throw new RepositoryOperationException(nameof(Update), $"Failed to update entity with ID {entity.Id}");
            }

            // Publish event with updated entity after update
            await _eventPublisher.Publish(updated, $"{typeof(T).Name}.Updated", cancellationToken);

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
            // Get the entity for history
            var entity = _collection.FindById(id);

            // Verify entity exists before deletion
            if (entity is null)
            {
                _logger.LogCritical("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for removal", nameof(Remove), _entityName, id);
                throw new EntityNotFoundException(id, _entityName);
            }

            var deleted = _collection.Delete(id);
            if (!deleted)
            {
                _logger.LogCritical("Critical error in {MethodName}: Failed to remove {EntityType} with ID {EntityId}", nameof(Remove), _entityName, id);
                throw new RepositoryOperationException(nameof(Remove), $"Failed to remove entity with ID {id}");
            }

            // Publish event after deletion
            await _eventPublisher.Publish(entity, $"{typeof(T).Name}.Deleted", cancellationToken);

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Remove), _entityName, id);
            throw;
        }
    }
}
