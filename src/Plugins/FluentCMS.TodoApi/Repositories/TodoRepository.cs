using FluentCMS.Repositories.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using FluentCMS.TodoApi.Models;
using Microsoft.Extensions.Logging;

namespace FluentCMS.TodoApi.Repositories;

public interface ITodoRepository : IRepository<Todo>
{
}

public class TodoRepository : Repository<Todo, TodoDbContext>, ITodoRepository
{
    public TodoRepository(TodoDbContext context, ILogger<TodoRepository> logger) : base(context)
    {
        logger.LogDebug("TodoRepository created");
        logger.LogDebug("TodoRepository created with context: {Context}", context.GetType().Name);
        logger.LogDebug("TodoRepository created with DbSet: {ProviderName}", context.Database.ProviderName);
    }
}