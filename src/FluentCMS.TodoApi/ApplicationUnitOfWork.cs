using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework;
using FluentCMS.TodoApi.Models;

namespace FluentCMS.TodoApi;

public interface IApplicationUnitOfWork : IUnitOfWork
{
    ITodoRepository TodoRepository { get; }

}

public class ApplicationUnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider) : UnitOfWork<ApplicationDbContext>(context, serviceProvider), IApplicationUnitOfWork
{
    public ITodoRepository TodoRepository => (ITodoRepository) Repository<Todo>();
}
