namespace FluentCMS.DataAccess.Abstractions;

public interface IEntityRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity> Remove(Guid id, CancellationToken cancellationToken = default);
}
