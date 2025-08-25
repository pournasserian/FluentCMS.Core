namespace FluentCMS.Repositories.Tests.Repositories;

public class TransactionalRepositoryTests(ServiceProviderFixture fixture) : IClassFixture<ServiceProviderFixture>
{
    [Fact]
    public async Task BeginTransaction_ShouldStartTransaction()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        // Act
        await repository.BeginTransaction();

        // Assert
        Assert.True(repository.IsTransactionActive);
    }

    [Fact]
    public async Task Commit_ShouldSaveChanges()
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
        await repository.Add(entity);

        // Act
        await repository.Commit();

        // Assert
        Assert.False(repository.IsTransactionActive);

        // Verify entity was saved
        var retrieved = await repository.GetById(entity.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(entity.Id, retrieved.Id);
    }

    [Fact]
    public async Task Rollback_ShouldDiscardChanges()
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
        await repository.Add(entity);

        // Act
        await repository.Rollback();

        // Assert
        Assert.False(repository.IsTransactionActive);

        // Verify entity was not saved
        var retrieved = await repository.GetById(entity.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task TransactionalOperations_ShouldWorkTogether()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        // Act & Assert
        await repository.BeginTransaction();
        Assert.True(repository.IsTransactionActive);

        var entity1 = new TestEntity { Name = "Entity 1", Value = 1 };
        var entity2 = new TestEntity { Name = "Entity 2", Value = 2 };

        var added1 = await repository.Add(entity1);
        var added2 = await repository.Add(entity2);

        await repository.Commit();
        Assert.False(repository.IsTransactionActive);

        // Verify both entities were saved
        var retrieved1 = await repository.GetById(added1.Id);
        var retrieved2 = await repository.GetById(added2.Id);

        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal("Entity 1", retrieved1.Name);
        Assert.Equal("Entity 2", retrieved2.Name);
    }

    [Fact]
    public async Task RollbackAfterPartialOperations_ShouldDiscardAll()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        await repository.BeginTransaction();
        var entity1 = new TestEntity { Name = "Entity 1", Value = 1 };
        var entity2 = new TestEntity { Name = "Entity 2", Value = 2 };

        await repository.Add(entity1);
        await repository.Add(entity2);

        // Act
        await repository.Rollback();

        // Assert
        Assert.False(repository.IsTransactionActive);

        // Verify no entities were saved
        var allEntities = await repository.GetAll();
        Assert.Empty(allEntities);
    }

    [Fact]
    public async Task BeginTransaction_WhenAlreadyActive_ShouldThrowException()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        await repository.BeginTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException<TestEntity>>(async () =>
        {
            await repository.BeginTransaction();
        });
    }

    [Fact]
    public async Task Commit_WhenNoActiveTransaction_ShouldThrowException()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException<TestEntity>>(async () =>
        {
            await repository.Commit();
        });
    }

    [Fact]
    public async Task Rollback_WhenNoActiveTransaction_ShouldThrowException()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException<TestEntity>>(async () =>
        {
            await repository.Rollback();
        });
    }
}
