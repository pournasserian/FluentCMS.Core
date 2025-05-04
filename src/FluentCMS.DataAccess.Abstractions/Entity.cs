namespace FluentCMS.DataAccess.Abstractions;

public abstract class Entity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
}

public abstract class Entity : Entity<Guid>, IEntity
{
}
