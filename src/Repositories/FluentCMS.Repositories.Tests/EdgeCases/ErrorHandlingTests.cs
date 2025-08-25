using System.Linq.Expressions;

namespace FluentCMS.Repositories.Tests.EdgeCases;

public class ErrorHandlingTests(ServiceProviderFixture fixture) : IClassFixture<ServiceProviderFixture>
{
    private readonly IServiceProvider _serviceProvider = fixture.ServiceProvider;

    [Fact]
    public async Task Add_NullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        TestEntity? nullEntity = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await repository.Add(nullEntity!);
        });
    }

    [Fact]
    public async Task Update_NullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        TestEntity? nullEntity = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await repository.Update(nullEntity!);
        });
    }

    [Fact]
    public async Task Remove_NullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        TestEntity? nullEntity = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await repository.Remove(nullEntity!);
        });
    }

    [Fact]
    public async Task Find_NullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        Expression<Func<TestEntity, bool>>? nullPredicate = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await repository.Find(nullPredicate!);
        });
    }

    [Fact]
    public async Task Remove_NonExistentEntity_ShouldThrowRepositoryException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var nonExistentEntity = new TestEntity { Id = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException<TestEntity>>(async () =>
        {
            await repository.Remove(nonExistentEntity);
        });
    }

    [Fact]
    public async Task Remove_NonExistentId_ShouldThrowRepositoryException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException<TestEntity>>(async () =>
        {
            await repository.Remove(Guid.NewGuid());
        });
    }

    [Fact]
    public async Task Transactional_Commit_WithoutBegin_ShouldThrowRepositoryException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException<TestEntity>>(async () =>
        {
            await repository.Commit();
        });
    }

    [Fact]
    public async Task Transactional_Rollback_WithoutBegin_ShouldThrowRepositoryException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionalRepository<TestEntity>>();

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException<TestEntity>>(async () =>
        {
            await repository.Rollback();
        });
    }

    [Fact]
    public async Task Concurrent_AddOperations_ShouldHandleGracefully()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        var tasks = new List<Task<TestEntity>>();
        for (int i = 0; i < 5; i++)
        {
            var entity = new TestEntity
            {
                Name = $"Concurrent Entity {i}",
                Description = $"Description {i}",
                Value = i
            };
            tasks.Add(repository.Add(entity));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, results.Length);
        Assert.All(results, r => Assert.NotEqual(Guid.Empty, r.Id));
    }

    [Fact]
    public async Task EmptyDatabase_Operations_ShouldWorkCorrectly()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        // Clear all existing data
        var allEntities = await repository.GetAll();
        var removeTasks = allEntities.Select(entity => repository.Remove(entity));
        await Task.WhenAll(removeTasks);

        // Act
        var allResult = await repository.GetAll();
        var countResult = await repository.Count();
        var anyResult = await repository.Any();

        // Assert
        Assert.Empty(allResult);
        Assert.Equal(0, countResult);
        Assert.False(anyResult);
    }

    [Fact]
    public async Task LargeDataSet_Operations_ShouldWorkEfficiently()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        var entities = new List<TestEntity>();
        for (int i = 0; i < 100; i++)
        {
            entities.Add(new TestEntity
            {
                Name = $"Entity {i}",
                Description = $"Description {i}",
                Value = i
            });
        }

        // Act
        await repository.AddMany(entities);
        var allEntities = await repository.GetAll();
        var count = await repository.Count();
        var filteredCount = await repository.Count(e => e.Value >= 50);

        // Assert
        Assert.True(allEntities.Count() >= 100);
        Assert.True(count >= 100);
        Assert.Equal(50, filteredCount);
    }
}
