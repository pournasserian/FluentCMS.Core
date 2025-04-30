
namespace FluentCMS.Core.Repositories.LiteDB;

public class EntityRepository<TEntity> : IEntityRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly ILiteCollection<TEntity> Collection;
    protected readonly ILiteDatabase Database;
    protected readonly string EntityName;
    protected readonly ILogger<EntityRepository<TEntity>> _logger;
    protected readonly IEventPublisher EventPublisher;

    public EntityRepository(ILiteDBContext dbContext, ILogger<EntityRepository<TEntity>> logger, IEventPublisher eventPublisher)
    {
        Database = dbContext.Database;
        EntityName = typeof(TEntity).Name;
        Collection = Database.GetCollection<TEntity>(EntityName);

        // Ensure we have an index on Id field
        Collection.EnsureIndex(x => x.Id);
        _logger = logger;
        EventPublisher = eventPublisher;
    }

    public virtual async Task<TEntity> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // LiteDB is synchronous, but we'll wrap in Task to comply with interface
            var entity = Collection.FindById(id);

            if (entity == null)
            {
                _logger.LogError("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found", nameof(GetById), EntityName, id);
                throw new EntityNotFoundException(id, EntityName);
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
            _logger.LogError(ex, "Critical error in {MethodName}: Error while retrieving {EntityType} with ID {EntityId}", nameof(GetById), EntityName, id);
            throw new RepositoryOperationException(nameof(GetById), ex);
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Use Query API for consistency with other methods
            var entities = Collection.Query().ToList();
            return await Task.FromResult(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: Error while retrieving all {EntityType} entities", nameof(GetAll), EntityName);
            throw new RepositoryOperationException(nameof(GetAll), ex);
        }
    }

    public virtual Task<IEnumerable<TEntity>> Find(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var entities = Collection.Query().Where(predicate).ToEnumerable() ?? [];
            return Task.FromResult(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: Error while querying {EntityType} entities", nameof(Find), EntityName);
            throw new RepositoryOperationException(nameof(Query), ex);
        }
    }

    public virtual async Task<QueryResult<TEntity>> Query(QueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Build the LiteDB query
            ILiteQueryable<TEntity> query = Collection.Query();

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
            var result = new QueryResult<TEntity>
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
            _logger.LogError(ex, "Critical error in {MethodName}: Error while querying {EntityType} entities", nameof(Query), EntityName);
            throw new RepositoryOperationException(nameof(Query), ex);
        }
    }

    public virtual async Task<long> Count(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Use LiteDB's Query API for counting to fully leverage database capabilities
            if (filter == null)
            {
                return await Task.FromResult(Collection.Count());
            }
            else
            {
                return await Task.FromResult(Collection.Query().Where(filter).Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: Error while counting {EntityType} entities", nameof(Count), EntityName);
            throw new RepositoryOperationException(nameof(Count), ex);
        }
    }

    public virtual async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            var inserted = Collection.Insert(entity);
            if (inserted == null)
            {
                _logger.LogError("Critical error in {MethodName}: Failed to add {EntityType} with ID {EntityId}", nameof(Add), EntityName, entity.Id);
                throw new RepositoryOperationException(nameof(Add), $"Failed to add entity with ID {entity.Id}");
            }

            // Publish event after successful addition
            await EventPublisher.Publish(entity, $"{typeof(TEntity).Name}.Created", cancellationToken);

            return entity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Add), EntityName, entity.Id);
            throw;
        }
    }

    public virtual async Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Get the original entity for history
            var originalEntity = Collection.FindById(entity.Id);

            // Verify entity exists before update
            if (originalEntity is null)
            {
                _logger.LogError("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for update", nameof(Update), EntityName, entity.Id);
                throw new EntityNotFoundException(entity.Id, EntityName);
            }

            var updated = Collection.Update(entity);
            if (!updated)
            {
                _logger.LogError("Critical error in {MethodName}: Failed to update {EntityType} with ID {EntityId}", nameof(Update), EntityName, entity.Id);
                throw new RepositoryOperationException(nameof(Update), $"Failed to update entity with ID {entity.Id}");
            }

            // Publish event with updated entity after update
            await EventPublisher.Publish(updated, $"{typeof(TEntity).Name}.Updated", cancellationToken);

            return entity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Update), EntityName, entity.Id);
            throw;
        }
    }

    public virtual async Task<TEntity> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Get the entity for history
            var entity = await GetById(id, cancellationToken);

            var deleted = Collection.Delete(id);
            if (!deleted)
            {
                _logger.LogError("Critical error in {MethodName}: Failed to remove {EntityType} with ID {EntityId}", nameof(Remove), EntityName, id);
                throw new RepositoryOperationException(nameof(Remove), $"Failed to remove entity with ID {id}");
            }

            // Publish event after deletion
            await EventPublisher.Publish(entity, $"{typeof(TEntity).Name}.Deleted", cancellationToken);

            return entity;

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Remove), EntityName, id);
            throw;
        }
    }

    public virtual Task<TEntity> Remove(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return Remove(entity.Id, cancellationToken);
    }

    public IQueryable<TEntity> AsQueryable()
    {
        try
        {
            return Collection.FindAll().AsQueryable();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Critical error in {MethodName}", nameof(AsQueryable));
            throw;
        }
    }
}
