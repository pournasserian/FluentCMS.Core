namespace FluentCMS.Core.Repositories.LiteDB;

public class AuditableEntityRepository<T>(ILiteDBContext dbContext, ILogger<AuditableEntityRepository<T>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : EntityRepository<T>(dbContext, logger, eventPublisher, executionContext), IAuditableEntityRepository<T> where T : class, IAuditableEntity
{
    public override async Task<T> Add(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = ExecutionContext.Username;
            entity.ModifiedBy = null;
            entity.ModifiedAt = null;
            entity.Version = 1;

            return await base.Add(entity, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Update), EntityName, entity.Id);
            throw;
        }
    }

    public override async Task<T> Update(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Get the original entity for history
            var originalEntity = Collection.FindById(entity.Id);

            // Verify entity exists before update
            if (originalEntity is null)
            {
                _logger.LogCritical("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for update", nameof(Update), EntityName, entity.Id);
                throw new EntityNotFoundException(entity.Id, EntityName);
            }

            entity.CreatedAt = originalEntity.CreatedAt;
            entity.CreatedBy = originalEntity.CreatedBy;
            entity.ModifiedBy = ExecutionContext.Username;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.Version = originalEntity.Version + 1;

            return await base.Update(entity, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Update), EntityName, entity.Id);
            throw;
        }
    }

    public override async Task<T> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Get the entity for history
            var entity = Collection.FindById(id);

            // Verify entity exists before deletion
            if (entity is null)
            {
                _logger.LogCritical("Critical error in {MethodName}: {EntityType} with ID {EntityId} not found for removal", nameof(Remove), EntityName, id);
                throw new EntityNotFoundException(id, EntityName);
            }

            var deleted = await base.Remove(id, cancellationToken);
            if (deleted is null)
            {
                _logger.LogCritical("Critical error in {MethodName}: Failed to remove {EntityType} with ID {EntityId}", nameof(Remove), EntityName, id);
                throw new RepositoryOperationException(nameof(Remove), $"Failed to remove entity with ID {id}");
            }
            return deleted;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Remove), EntityName, id);
            throw;
        }
    }
}
