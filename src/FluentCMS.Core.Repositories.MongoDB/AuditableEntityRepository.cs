namespace FluentCMS.Core.Repositories.MongoDB;

public abstract class AuditableEntityRepository<TEntity>(IMongoDBContext dbContext, ILogger<AuditableEntityRepository<TEntity>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : EntityRepository<TEntity>(dbContext, logger, eventPublisher), IAuditableEntityRepository<TEntity> where TEntity : class, IAuditableEntity
{
    protected readonly IApplicationExecutionContext ExecutionContext = executionContext;

    public override async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = ExecutionContext.Username;
        entity.ModifiedBy = null;
        entity.ModifiedAt = null;
        entity.Version = 1;

        return await base.Add(entity, cancellationToken);
    }

    public override async Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Get the original entity for history
            var originalEntity = await GetById(entity.Id, cancellationToken);

            entity.CreatedAt = originalEntity.CreatedAt;
            entity.CreatedBy = originalEntity.CreatedBy;
            entity.ModifiedBy = ExecutionContext.Username;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.Version = originalEntity.Version + 1;

            var idFilter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
            var replaceResult = await Collection.ReplaceOneAsync(idFilter, entity, cancellationToken: cancellationToken);

            if (replaceResult?.ModifiedCount != 1)
            {
                _logger.LogError("Critical error in {MethodName}: Failed to update {EntityType} with ID {EntityId}", nameof(Update), EntityName, entity.Id);
                throw new RepositoryOperationException(nameof(Update), $"Failed to update entity with ID {entity.Id}");
            }

            // Publish event with updated entity after update
            await EventPublisher.Publish(entity, $"{typeof(TEntity).Name}.Updated", cancellationToken);

            return entity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Critical error in {MethodName}: {EntityType} with ID {EntityId}", nameof(Update), EntityName, entity.Id);
            throw;
        }
    }
}
