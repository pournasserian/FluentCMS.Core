namespace FluentCMS.DataAccess.Abstractions;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class, IEntity;
    Task SaveChanges(CancellationToken cancellationToken = default);
}
