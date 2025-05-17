namespace FluentCMS.Repositories.EntityFramework;

public interface IAuditableEntityInterceptorDbContext
{
    // Marker interface for DbContext that should use the AuditableEntityInterceptor
}

public interface IAutoIdGeneratorDbContext
{
    // Marker interface for DbContext that should use the AutoIdGeneratorInterceptor
}

public interface IEventPublisherDbContext
{
    // Marker interface for DbContext that should use the EventPublisherInterceptor
}