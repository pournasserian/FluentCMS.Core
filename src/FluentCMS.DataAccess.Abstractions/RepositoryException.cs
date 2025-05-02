namespace FluentCMS.DataAccess.Abstractions;

public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException(string id, string entityName) : RepositoryException($"Entity of type {entityName} with id {id} was not found.")
{
}

public class RepositoryOperationException : RepositoryException
{
    public RepositoryOperationException(string operation, string message) : base($"Repository operation '{operation}' failed: {message}") { }

    public RepositoryOperationException(string operation, Exception innerException) : base($"Repository operation '{operation}' failed.", innerException) { }
}
