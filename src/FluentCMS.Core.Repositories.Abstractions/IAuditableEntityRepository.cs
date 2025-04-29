namespace FluentCMS.Core.Repositories.Abstractions;

public interface IAuditableEntityRepository<T> : IEntityRepository<T> where T : class, IAuditableEntity
{
}
