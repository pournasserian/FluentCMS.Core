using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework;
using FluentCMS.TodoApi.Models;

namespace FluentCMS.TodoApi;

public interface ITodoRepository : IRepository<Todo>
{
}

public class TodoRepository(ApplicationDbContext context) : Repository<Todo>(context), ITodoRepository
{
}