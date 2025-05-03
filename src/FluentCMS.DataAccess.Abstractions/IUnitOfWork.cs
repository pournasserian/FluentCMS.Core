namespace FluentCMS.DataAccess.Abstractions;

public interface IUnitOfWork<TContext> : IDisposable
{
    T Repository<T>() where T : IRepository;
    Task SaveChanges(CancellationToken cancellationToken = default);

    // Get the actual DbContext
    TContext Context { get; }
}
