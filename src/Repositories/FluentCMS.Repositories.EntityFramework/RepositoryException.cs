namespace FluentCMS.Repositories.EntityFramework;

[Serializable]
public class RepositoryException<TEntity>(string message, Exception? innerException) : EnhancedException($"Repository", message, innerException) where TEntity : class, IEntity
{
    public RepositoryException(string message) : this(message, null)
    {
    }
}