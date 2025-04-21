using FluentCMS.Core.EventBus;
using FluentCMS.Core.Repositories.Abstractions;
using LiteDB;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

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
            var entities = _collection.FindAll();
            return await Task.FromResult(entities);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: Error while retrieving all {EntityType} entities", nameof(GetAll), _entityName);
            throw new RepositoryOperationException(nameof(GetAll), ex);
        }
    }

    public async Task<IEnumerable<T>> Query(Expression<Func<T, bool>>? filter = default, PaginationOptions? paginationOptions = default, IList<SortOption<T>>? sortOptions = default, CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<T>> Query(QueryOptions<T> options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Initialize query with filter or all entities
            IEnumerable<T> query = options.Filter == null
                ? _collection.FindAll()
                : _collection.Find(options.Filter);

            // Apply sorting if provided
            if (options.Sorting != null && options.Sorting.Any())
            {
                var firstSort = options.Sorting.First();
                var orderedQuery = firstSort.Direction == SortDirection.Ascending
                    ? query.OrderBy(firstSort.KeySelector.Compile())
                    : query.OrderByDescending(firstSort.KeySelector.Compile());

                foreach (var sortOption in options.Sorting.Skip(1))
                {
                    orderedQuery = sortOption.Direction == SortDirection.Ascending
                        ? orderedQuery.ThenBy(sortOption.KeySelector.Compile())
                        : orderedQuery.ThenByDescending(sortOption.KeySelector.Compile());
                }

                query = orderedQuery;
            }

            // Apply pagination if provided
            if (options.Pagination != null)
            {
                query = query.Skip(options.Pagination.Skip).Take(options.Pagination.PageSize);
            }

            return await Task.FromResult(query.ToList());
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
            int count = filter == null ? _collection.Count() : _collection.Count(filter);

            return await Task.FromResult(count);
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

            var inserted = _collection.Insert(entity);
            if (inserted == null)
            {
                _logger.LogCritical("Critical error in {MethodName}: Failed to add {EntityType} with ID {EntityId}",
                    nameof(Add), _entityName, entity.Id);
                throw new RepositoryOperationException(nameof(Add), $"Failed to add entity with ID {entity.Id}");
            }

            // Publish event after successful addition
            await _eventPublisher.Publish(entity, $"{typeof(T).Name}.Created", cancellationToken);

            return entity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}",
                nameof(Add), _entityName, entity.Id);
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
                _logger.LogCritical("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for update",
                    nameof(Update), _entityName, entity.Id);
                throw new EntityNotFoundException(entity.Id, _entityName);
            }

            // Get the original entity for history
            var originalEntity = _collection.FindById(entity.Id);

            // Publish event with original entity before update
            await _eventPublisher.Publish(originalEntity, $"{typeof(T).Name}.Updated", cancellationToken);

            var updated = _collection.Update(entity);
            if (!updated)
            {
                _logger.LogCritical("Critical error in {MethodName}: Failed to update {EntityType} with ID {EntityId}",
                    nameof(Update), _entityName, entity.Id);
                throw new RepositoryOperationException(nameof(Update), $"Failed to update entity with ID {entity.Id}");
            }

            return entity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}",
                nameof(Update), _entityName, entity.Id);
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
                _logger.LogCritical("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for removal",
                    nameof(Remove), _entityName, id);
                throw new EntityNotFoundException(id, _entityName);
            }

            // Get the entity for history
            var entity = _collection.FindById(id);

            // Publish event before deletion
            await _eventPublisher.Publish(entity, $"{typeof(T).Name}.Deleted", cancellationToken);

            var deleted = _collection.Delete(id);
            if (!deleted)
            {
                _logger.LogCritical("Critical error in {MethodName}: Failed to remove {EntityType} with ID {EntityId}",
                    nameof(Remove), _entityName, id);
                throw new RepositoryOperationException(nameof(Remove), $"Failed to remove entity with ID {id}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}",
                nameof(Remove), _entityName, id);
            throw;
        }
    }
}
