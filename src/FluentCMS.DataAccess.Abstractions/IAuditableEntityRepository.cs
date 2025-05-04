namespace FluentCMS.DataAccess.Abstractions;

public interface IAuditableEntityRepository<TEntity> : IEntityRepository<TEntity> where TEntity : class, IAuditableEntity
{
}
