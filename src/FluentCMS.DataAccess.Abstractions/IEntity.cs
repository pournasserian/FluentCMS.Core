namespace FluentCMS.DataAccess.Abstractions;

public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; set; }
}

public interface IEntity : IEntity<Guid>
{
}
