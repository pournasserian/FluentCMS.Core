using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataAccess.EntityFramework;

public interface IUnitOfWorkFactory
{
    IUnitOfWork<TContext> Create<TContext>() where TContext : DbContext;
}

public class UnitOfWorkFactory(IServiceProvider serviceProvider) : IUnitOfWorkFactory
{
    public IUnitOfWork<TContext> Create<TContext>() where TContext : DbContext
    {
        return serviceProvider.GetRequiredService<IUnitOfWork<TContext>>();
    }
}
