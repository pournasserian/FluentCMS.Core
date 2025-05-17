namespace FluentCMS.Repositories.EntityFramework.Interceptors;

public class IdGeneratorInterceptor : BaseSaveChangesInterceptor<IAutoIdGeneratorDbContext>
{
    public override Task BeforeSaveChanges(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        foreach (var entry in eventData.Context!.ChangeTracker.Entries<IEntity>())
        {
            var entity = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entity.Id == Guid.Empty)
                        entity.Id = Guid.NewGuid();
                    break;
            }
        }
        return Task.CompletedTask;
    }
}