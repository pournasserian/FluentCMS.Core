using System.Linq.Expressions;
using FluentCMS.Core.Repositories.Abstractions;
using LiteDB;

namespace FluentCMS.Core.Repositories.LiteDB;

public class LiteDBRepository<T> : IBaseEntityRepository<T> where T : IBaseEntity
{
    private readonly ILiteCollection<T> _collection;
    private readonly ILiteDatabase _database;
    private readonly string _entityName;

    public LiteDBRepository(ILiteDBContext dbContext)
    {
        _database = dbContext.Database;
        _entityName = typeof(T).Name;
        _collection = _database.GetCollection<T>(_entityName);

        // Ensure we have an index on Id field
        _collection.EnsureIndex(x => x.Id);
    }

    public async Task<T?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // LiteDB is synchronous, but we'll wrap in Task to comply with interface
            var entity = _collection.FindById(id);
            return await Task.FromResult(entity);
        }
        catch (Exception ex)
        {
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
            throw new RepositoryOperationException(nameof(GetAll), ex);
        }
    }

    public async Task<IEnumerable<T>> Query(Expression<Func<T, bool>>? filter = default, PaginationOptions? paginationOptions = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Start with all entities or apply filter if provided
            var query = filter == null ? _collection.FindAll() : _collection.Find(filter);

            // Apply pagination if provided
            if (paginationOptions != null)
            {
                query = query.Skip(paginationOptions.Skip).Take(paginationOptions.PageSize);
            }

            return await Task.FromResult(query.ToList());
        }
        catch (Exception ex)
        {
            throw new RepositoryOperationException(nameof(Query), ex);
        }
    }

    public async Task<IEnumerable<T>> Query(QueryOptions<T> options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Query(options.Filter, options.Pagination, cancellationToken);
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
            throw new RepositoryOperationException(nameof(Count), ex);
        }
    }

    public async Task<T> Add(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        var inserted = _collection.Insert(entity);
        if (inserted == null)
        {
            throw new RepositoryOperationException(nameof(Add), $"Failed to add entity with ID {entity.Id}");
        }
        return await Task.FromResult(entity);
    }

    public async Task<T> Update(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Verify entity exists before update
        if (!_collection.Exists(e => e.Id == entity.Id))
        {
            throw new EntityNotFoundException(entity.Id, _entityName);
        }

        var updated = _collection.Update(entity);
        if (!updated)
        {
            throw new RepositoryOperationException(nameof(Update), $"Failed to update entity with ID {entity.Id}");
        }

        return await Task.FromResult(entity);
    }

    public async Task Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Verify entity exists before deletion
        if (!_collection.Exists(e => e.Id == id))
        {
            throw new EntityNotFoundException(id, _entityName);
        }

        var deleted = _collection.Delete(id);
        if (!deleted)
        {
            throw new RepositoryOperationException(nameof(Remove), $"Failed to remove entity with ID {id}");
        }

        await Task.CompletedTask;
    }
}