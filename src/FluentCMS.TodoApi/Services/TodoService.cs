using FluentCMS.TodoApi.Models;

namespace FluentCMS.TodoApi.Services;

public interface ITodoService
{
    Task<Todo> Add(Todo entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<Todo>> GetAll(CancellationToken cancellationToken = default);
    Task<Todo?> GetById(Guid entityId, CancellationToken cancellationToken = default);
    Task Remove(Guid entityId, CancellationToken cancellationToken = default);
    Task<Todo> Update(Todo entity, CancellationToken cancellationToken = default);
}

public class TodoService(IApplicationUnitOfWork unitOfWork) : ITodoService
{
    public async Task<Todo> Add(Todo entity, CancellationToken cancellationToken = default)
    {
        await unitOfWork.TodoRepository.Add(entity, cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);
        return entity;
    }

    public async Task<Todo> Update(Todo entity, CancellationToken cancellationToken = default)
    {
        await unitOfWork.TodoRepository.Update(entity, cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);
        return entity;
    }

    public async Task Remove(Guid entityId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.TodoRepository.Remove(entityId, cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);
    }

    public Task<Todo?> GetById(Guid entityId, CancellationToken cancellationToken = default)
    {
        return unitOfWork.TodoRepository.GetById(entityId, cancellationToken);
    }

    public Task<IEnumerable<Todo>> GetAll(CancellationToken cancellationToken = default)
    {
        return unitOfWork.TodoRepository.GetAll(cancellationToken);
    }
}
