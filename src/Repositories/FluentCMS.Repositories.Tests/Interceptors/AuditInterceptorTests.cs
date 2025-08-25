namespace FluentCMS.Repositories.Tests.Interceptors;

public class AuditInterceptorTests(ServiceProviderFixture fixture) : IClassFixture<ServiceProviderFixture>
{
    private readonly IServiceProvider _serviceProvider = fixture.ServiceProvider;

    [Fact]
    public async Task Add_Entity_ShouldSetAuditProperties()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            Value = 42
        };

        // Act
        var result = await repository.Add(entity);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.False(string.IsNullOrEmpty(result.CreatedBy));
        Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
        Assert.Equal(1, result.Version);
        Assert.True(string.IsNullOrEmpty(result.UpdatedBy));
        Assert.Null(result.UpdatedAt);
    }

    [Fact]
    public async Task Update_Entity_ShouldUpdateAuditProperties()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var entity = new TestEntity
        {
            Name = "Original Name",
            Description = "Original Description",
            Value = 42
        };
        var addedEntity = await repository.Add(entity);

        // Store original audit values
        var originalCreatedBy = addedEntity.CreatedBy;
        var originalCreatedAt = addedEntity.CreatedAt;
        var originalVersion = addedEntity.Version;

        // Modify the entity
        addedEntity.Name = "Updated Name";

        // Act
        var result = await repository.Update(addedEntity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(originalCreatedBy, result.CreatedBy);
        Assert.Equal(originalCreatedAt, result.CreatedAt);
        Assert.Equal(originalVersion + 1, result.Version);
        Assert.False(string.IsNullOrEmpty(result.UpdatedBy));
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task AddMany_Entities_ShouldSetAuditProperties()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var entities = new List<TestEntity>
        {
            new() { Name = "Entity 1", Description = "Description 1", Value = 1 },
            new() { Name = "Entity 2", Description = "Description 2", Value = 2 }
        };

        // Act
        var result = await repository.AddMany(entities);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());

        foreach (var entity in result)
        {
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.False(string.IsNullOrEmpty(entity.CreatedBy));
            Assert.NotEqual(DateTime.MinValue, entity.CreatedAt);
            Assert.Equal(1, entity.Version);
            Assert.True(string.IsNullOrEmpty(entity.UpdatedBy));
            Assert.Null(entity.UpdatedAt);
        }
    }

    [Fact]
    public async Task Transactional_Add_ShouldSetAuditProperties()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        await repository.BeginTransaction();
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            Value = 42
        };

        // Act
        var result = await repository.Add(entity);
        await repository.Commit();

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.False(string.IsNullOrEmpty(result.CreatedBy));
        Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task Transactional_Update_ShouldUpdateAuditProperties()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        // Add entity first
        var entity = new TestEntity
        {
            Name = "Original Name",
            Description = "Original Description",
            Value = 42
        };
        var addedEntity = await repository.Add(entity);

        // Start transaction for update
        await repository.BeginTransaction();
        addedEntity.Name = "Updated Name";

        // Act
        var result = await repository.Update(addedEntity);
        await repository.Commit();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.False(string.IsNullOrEmpty(result.UpdatedBy));
        Assert.NotNull(result.UpdatedAt);
    }
}
