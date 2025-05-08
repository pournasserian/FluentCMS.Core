using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework;
using FluentCMS.TodoApi.Models;

namespace FluentCMS.TodoApi.Repositories;

public interface ITodoRepository : IRepository<Todo>
{
}

public class TodoRepository(TodoDbContext context) : Repository<Todo, TodoDbContext>(context), ITodoRepository
{
}