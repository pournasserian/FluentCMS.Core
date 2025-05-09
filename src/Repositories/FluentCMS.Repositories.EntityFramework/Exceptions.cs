namespace FluentCMS.Repositories.EntityFramework;

[Serializable]
public class RepositoryException<TEntity> : Exception where TEntity : class, IEntity
{
    public RepositoryException(string message, Exception? innerException) : base(message, innerException)
    {
    }
    public RepositoryException(string message) : this(message, null)
    {
    }
}