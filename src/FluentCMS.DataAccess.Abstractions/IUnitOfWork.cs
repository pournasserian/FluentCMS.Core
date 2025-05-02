namespace FluentCMS.DataAccess.Abstractions;

public interface IUnitOfWork : IDisposable
{
    T Repository<T>() where T : IRepository;
    Task SaveChanges(CancellationToken cancellationToken = default);
}
