using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework;

public class UnitOfWork(DbContext context) : IUnitOfWork
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public T Repository<T>() where T : IRepository
    {
        throw new NotImplementedException();
    }

    public Task SaveChanges(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

