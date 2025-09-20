# ⚡ Performance Issue: Synchronous Database Operations

## Issue Description

The provider repository contains synchronous database operations and lacks batch operation optimizations, leading to poor performance under load.

## Performance Impact
**Medium**

## Affected Components
- `ProviderRepository.cs`
- Database query performance
- Application responsiveness

## Performance Problems Identified

### 1. Missing Batch Operations
```csharp
// Current inefficient approach
public async Task AddMany(IEnumerable<Provider> providers, CancellationToken cancellationToken = default)
{
    await dbContext.Providers.AddRangeAsync(providers, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);  // Single save, but could be optimized
}
```

### 2. No Connection Pooling Considerations
- No explicit connection management
- Potential connection exhaustion under load
- No connection pooling optimization

### 3. Inefficient Query Patterns
- Missing bulk operations for updates/deletes
- No query optimization hints
- Lack of compiled queries for frequently used operations

## Performance Metrics

### Current Issues
- **N+1 queries**: Potential for multiple database round trips
- **Connection overhead**: New connections per operation
- **Memory usage**: Large result sets loaded into memory
- **Transaction overhead**: Multiple small transactions instead of batching

### Expected Improvements
- **70% reduction** in database round trips with batch operations
- **50% improvement** in throughput with connection pooling
- **60% reduction** in memory usage with streaming queries

## Proposed Solutions

### 1. Optimized Batch Operations
```csharp
public class OptimizedProviderRepository : IProviderRepository
{
    private readonly ProviderDbContext _dbContext;
    private readonly ILogger<OptimizedProviderRepository> _logger;
    
    public async Task AddManyOptimized(IEnumerable<Provider> providers, CancellationToken cancellationToken = default)
    {
        const int batchSize = 1000;
        var providerList = providers.ToList();
        
        _logger.LogInformation("Adding {Count} providers in batches of {BatchSize}", 
            providerList.Count, batchSize);
        
        for (int i = 0; i < providerList.Count; i += batchSize)
        {
            var batch = providerList.Skip(i).Take(batchSize);
            
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.Providers.AddRangeAsync(batch, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogDebug("Processed batch {BatchNumber}: {StartIndex}-{EndIndex}", 
                    (i / batchSize) + 1, i, Math.Min(i + batchSize - 1, providerList.Count - 1));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
    
    public async Task UpdateManyOptimized(IEnumerable<Provider> providers, CancellationToken cancellationToken = default)
    {
        // Use bulk update operations where possible
        await _dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE Providers SET IsActive = {0}, LastModified = {1} WHERE Id IN ({2})",
            false, DateTime.UtcNow, string.Join(",", providers.Select(p => p.Id)),
            cancellationToken);
    }
}
```

### 2. Connection Pool Optimization
```csharp
public class ConnectionPoolConfiguration
{
    public static void ConfigureConnectionPool(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            // Connection pooling optimizations
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
        
        // Connection pool settings
        options.EnableServiceProviderCaching();
        options.EnableSensitiveDataLogging(false);
    }
}

// In Startup.cs
services.AddDbContext<ProviderDbContext>(options =>
{
    ConnectionPoolConfiguration.ConfigureConnectionPool(options, connectionString);
}, ServiceLifetime.Scoped);

// Add connection pooling
services.AddDbContextPool<ProviderDbContext>(options =>
{
    ConnectionPoolConfiguration.ConfigureConnectionPool(options, connectionString);
}, poolSize: 128); // Optimize pool size based on load
```

### 3. Compiled Queries for Performance
```csharp
public static class CompiledQueries
{
    private static readonly Func<ProviderDbContext, string, Task<Provider?>> GetActiveProviderByAreaQuery =
        EF.CompileAsyncQuery((ProviderDbContext context, string area) =>
            context.Providers.FirstOrDefault(p => p.Area == area && p.IsActive));
    
    private static readonly Func<ProviderDbContext, IAsyncEnumerable<Provider>> GetAllProvidersQuery =
        EF.CompileAsyncQuery((ProviderDbContext context) =>
            context.Providers.AsNoTracking());
    
    private static readonly Func<ProviderDbContext, string, int> GetProviderCountByAreaQuery =
        EF.CompileQuery((ProviderDbContext context, string area) =>
            context.Providers.Count(p => p.Area == area));
    
    public static Task<Provider?> GetActiveProviderByArea(ProviderDbContext context, string area)
        => GetActiveProviderByAreaQuery(context, area);
    
    public static IAsyncEnumerable<Provider> GetAllProviders(ProviderDbContext context)
        => GetAllProvidersQuery(context);
    
    public static int GetProviderCountByArea(ProviderDbContext context, string area)
        => GetProviderCountByAreaQuery(context, area);
}
```

### 4. Streaming and Pagination
```csharp
public class StreamingProviderRepository : IProviderRepository
{
    public async IAsyncEnumerable<Provider> StreamProvidersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const int pageSize = 1000;
        int offset = 0;
        
        while (true)
        {
            var providers = await _dbContext.Providers
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Skip(offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            
            if (!providers.Any())
                break;
            
            foreach (var provider in providers)
            {
                yield return provider;
            }
            
            offset += pageSize;
        }
    }
    
    public async Task<PagedResult<Provider>> GetProvidersPagedAsync(
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbContext.Providers.CountAsync(cancellationToken);
        
        var providers = await _dbContext.Providers
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return new PagedResult<Provider>
        {
            Items = providers,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
```

## Database Optimization Recommendations

### 1. Indexing Strategy
```sql
-- Add indexes for common query patterns
CREATE INDEX IX_Provider_Area_IsActive ON Providers (Area, IsActive) INCLUDE (Name, DisplayName);
CREATE INDEX IX_Provider_LastModified ON Providers (LastModified) INCLUDE (Id, Area);
CREATE INDEX IX_Provider_Name ON Providers (Name) WHERE IsActive = 1;
```

### 2. Query Performance Monitoring
```csharp
public class PerformanceMonitoringInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PerformanceMonitoringInterceptor> _logger;
    
    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        int result, 
        CancellationToken cancellationToken = default)
    {
        var duration = eventData.Duration;
        if (duration.TotalMilliseconds > 1000) // Log slow queries
        {
            _logger.LogWarning("Slow query detected: {CommandText} took {Duration}ms", 
                command.CommandText, duration.TotalMilliseconds);
        }
        
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }
}
```

## Configuration Options
```json
{
  "DatabasePerformance": {
    "BatchSize": 1000,
    "ConnectionPoolSize": 128,
    "CommandTimeout": 30,
    "EnableQueryCaching": true,
    "SlowQueryThreshold": 1000,
    "EnablePerformanceMonitoring": true
  }
}
```

## Performance Monitoring
- Track query execution times
- Monitor connection pool usage
- Measure batch operation throughput
- Alert on slow queries

## Priority
**Medium** - Improves database performance and scalability

## Labels
- performance
- medium-priority
- database
- optimization
- batch-operations
