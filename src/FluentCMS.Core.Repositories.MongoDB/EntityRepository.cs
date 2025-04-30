namespace FluentCMS.Core.Repositories.MongoDB;

public abstract class EntityRepository<TEntity> : IEntityRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly IMongoCollection<TEntity> Collection;
    protected readonly IMongoDatabase MongoDatabase;
    protected readonly IMongoDBContext MongoDbContext;
    protected readonly ILogger<EntityRepository<TEntity>> _logger;
    protected readonly IEventPublisher EventPublisher;
    protected readonly string EntityName;

    public EntityRepository(IMongoDBContext dbContext, ILogger<EntityRepository<TEntity>> logger, IEventPublisher eventPublisher)
    {
        EntityName = typeof(TEntity).Name;
        MongoDatabase = dbContext.Database;
        Collection = dbContext.Database.GetCollection<TEntity>(EntityName);
        MongoDbContext = dbContext;

        // Ensure index on Id field
        var indexKeysDefinition = Builders<TEntity>.IndexKeys.Ascending(x => x.Id);
        Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(indexKeysDefinition));

        _logger = logger;
        EventPublisher = eventPublisher;
    }

    public virtual async Task<TEntity> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // LiteDB is synchronous, but we'll wrap in Task to comply with interface
            //var entity = Collection.FindById(id);
            var idFilter = Builders<TEntity>.Filter.Eq(x => x.Id, id);
            var findResult = await Collection.FindAsync(idFilter, null, cancellationToken);
            var entity = await findResult.SingleOrDefaultAsync(cancellationToken);

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
            var filter = Builders<TEntity>.Filter.Empty;
            var findResult = await Collection.FindAsync(filter, null, cancellationToken);
            return await findResult.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: Error while retrieving all {EntityType} entities", nameof(GetAll), EntityName);
            throw new RepositoryOperationException(nameof(GetAll), ex);
        }
    }

    public virtual async Task<QueryResult<TEntity>> Query(QueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Build the LiteDB query
            IQueryable<TEntity> query = Collection.AsQueryable();

            // Apply filter if provided
            if (options.Filter != null)
                query = query.Where(options.Filter);

            // Calculate total count before applying pagination and sorting
            int totalCount = query.Count();

            // Apply sorting if provided
            if (options.Sorting != null && options.Sorting.Any())
            {
                // todo implement sorting
            }

            // Execute query and return results
            var result = new QueryResult<TEntity>
            {
                TotalCount = totalCount
            };

            // Apply pagination if provided
            if (options.Pagination == null)
                result.Items = query;
            else
                result.Items = query.Skip(options.Pagination.Skip).Take(options.Pagination.PageSize);

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
                return await Collection.EstimatedDocumentCountAsync(null, cancellationToken);
            }
            else
            {
                return await Task.FromResult(Collection.AsQueryable().Where(filter).Count());
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

        try
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            var inserted = Collection.InsertOneAsync(entity, null, cancellationToken);
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

        try
        {
            // Get the original entity for history
            var originalEntity = await GetById(entity.Id, cancellationToken);

            var idFilter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
            var replaceResult = await Collection.ReplaceOneAsync(idFilter, entity, cancellationToken: cancellationToken);

            if (replaceResult?.ModifiedCount != 1)
            {
                _logger.LogError("Critical error in {MethodName}: Failed to update {EntityType} with ID {EntityId}", nameof(Update), EntityName, entity.Id);
                throw new RepositoryOperationException(nameof(Update), $"Failed to update entity with ID {entity.Id}");
            }

            // Publish event with updated entity after update
            await EventPublisher.Publish(entity, $"{typeof(TEntity).Name}.Updated", cancellationToken);

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

            var idFilter = Builders<TEntity>.Filter.Eq(x => x.Id, id);
            var deleted = await Collection.FindOneAndDeleteAsync(idFilter, null, cancellationToken);
            if (deleted == null)
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
        return Remove(entity.Id, cancellationToken);
    }

    public IQueryable<TEntity> AsQueryable()
    {
        try
        {
            return Collection.AsQueryable();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Critical error in {MethodName}", nameof(AsQueryable));
            throw;
        }
    }
}
