using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.TodoApi.Models;

namespace FluentCMS.TodoApi.Services;

public interface ITodoService
{
    Task<Todo> Add(Todo entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<Todo>> GetAll(CancellationToken cancellationToken = default);
    Task<Todo> GetById(Guid entityId, CancellationToken cancellationToken = default);
    Task Remove(Guid entityId, CancellationToken cancellationToken = default);
    Task<Todo> Update(Todo entity, CancellationToken cancellationToken = default);
}

public class TodoService(IBaseEntityRepository<Todo> repository) : ITodoService
{
    public Task<Todo> Add(Todo entity, CancellationToken cancellationToken = default)
    {
        return repository.Add(entity, cancellationToken);
    }

    public Task<Todo> Update(Todo entity, CancellationToken cancellationToken = default)
    {
        return repository.Update(entity, cancellationToken);
    }

    public Task Remove(Guid entityId, CancellationToken cancellationToken = default)
    {
        return repository.Remove(entityId, cancellationToken);
    }

    public Task<Todo> GetById(Guid entityId, CancellationToken cancellationToken = default)
    {
        return repository.GetById(entityId, cancellationToken);
    }

    public Task<IEnumerable<Todo>> GetAll(CancellationToken cancellationToken = default)
    {
        return repository.GetAll(cancellationToken);
    }
}
