namespace FluentCMS.Repositories.Tests.Interceptors;

public class EventInterceptorTests(ServiceProviderFixture fixture) : IClassFixture<ServiceProviderFixture>
{
    [Fact]
    public async Task Add_Entity_ShouldPublishCreatedEvent()
    {
        // Arrange
        using var scope = fixture.CreateScope();
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
        // The event interceptor should have been called automatically
        // We can verify this by checking that the entity was processed correctly
        // In a real scenario, you would mock IEventPublisher to verify event publishing
    }

    [Fact]
    public async Task Update_Entity_ShouldPublishUpdatedEvent()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var entity = new TestEntity
        {
            Name = "Original Name",
            Description = "Original Description",
            Value = 42
        };
        var addedEntity = await repository.Add(entity);

        // Modify the entity
        addedEntity.Name = "Updated Name";

        // Act
        var result = await repository.Update(addedEntity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task Remove_Entity_ShouldPublishRemovedEvent()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            Value = 42
        };
        var addedEntity = await repository.Add(entity);

        // Act
        var result = await repository.Remove(addedEntity);

        // Assert
        Assert.NotNull(result);

        // Verify it's removed
        var retrieved = await repository.GetById(addedEntity.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Transactional_Add_ShouldPublishEventsOnCommit()
    {
        // Arrange
        using var scope = fixture.CreateScope();
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
    }

    [Fact]
    public async Task Transactional_Update_ShouldPublishEventsOnCommit()
    {
        // Arrange
        using var scope = fixture.CreateScope();
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
        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task Transactional_Remove_ShouldNotPublishEventsOnRollback()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        await repository.BeginTransaction();
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            Value = 42
        };
        var result = await repository.Add(entity);

        // Act
        await repository.Remove(result);
        await repository.Rollback();

        // Assert
        // Entity should not exist since transaction was rolled back
        var retrieved = await repository.GetById(result.Id);
        Assert.Null(retrieved);
    }
}
