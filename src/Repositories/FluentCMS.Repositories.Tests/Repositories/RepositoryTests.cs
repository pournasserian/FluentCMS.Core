namespace FluentCMS.Repositories.Tests.Repositories;

public class RepositoryTests(ServiceProviderFixture fixture) : IClassFixture<ServiceProviderFixture>
{
    private readonly IServiceProvider _serviceProvider = fixture.ServiceProvider;

    [Fact]
    public async Task Add_ShouldAddEntityAndReturnIt()
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
        Assert.Equal(entity.Name, result.Name);
        Assert.Equal(entity.Description, result.Description);
        Assert.Equal(entity.Value, result.Value);
    }

    [Fact]
    public async Task AddMany_ShouldAddMultipleEntities()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var entities = new List<TestEntity>
        {
            new() { Name = "Entity 1", Description = "Description 1", Value = 1 },
            new() { Name = "Entity 2", Description = "Description 2", Value = 2 },
            new() { Name = "Entity 3", Description = "Description 3", Value = 3 }
        };

        // Act
        var result = await repository.AddMany(entities);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.All(result, e => Assert.NotEqual(Guid.Empty, e.Id));
    }

    [Fact]
    public async Task GetById_ShouldReturnEntityWhenFound()
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
        var addedEntity = await repository.Add(entity);

        // Act
        var result = await repository.GetById(addedEntity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addedEntity.Id, result.Id);
        Assert.Equal(entity.Name, result.Name);
    }

    [Fact]
    public async Task GetById_ShouldReturnNullWhenNotFound()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        // Act
        var result = await repository.GetById(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllEntities()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        // Add some test entities
        await repository.Add(new TestEntity { Name = "Entity 1", Value = 1 });
        await repository.Add(new TestEntity { Name = "Entity 2", Value = 2 });

        // Act
        var result = await repository.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 2);
    }

    [Fact]
    public async Task Find_ShouldReturnMatchingEntities()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        // Add test entities
        await repository.Add(new TestEntity { Name = "Test Entity 1", Value = 10 });
        await repository.Add(new TestEntity { Name = "Test Entity 2", Value = 20 });
        await repository.Add(new TestEntity { Name = "Other Entity", Value = 10 });

        // Act
        var result = await repository.Find(e => e.Value == 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Equal(10, e.Value));
    }

    [Fact]
    public async Task Update_ShouldUpdateEntity()
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

        // Modify the entity
        addedEntity.Name = "Updated Name";
        addedEntity.Value = 99;

        // Act
        var result = await repository.Update(addedEntity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public async Task Remove_ByEntity_ShouldRemoveEntity()
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
        var addedEntity = await repository.Add(entity);

        // Act
        var result = await repository.Remove(addedEntity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addedEntity.Id, result.Id);

        // Verify it's removed
        var retrieved = await repository.GetById(addedEntity.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Remove_ById_ShouldRemoveEntity()
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
        var addedEntity = await repository.Add(entity);

        // Act
        var result = await repository.Remove(addedEntity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addedEntity.Id, result.Id);

        // Verify it's removed
        var retrieved = await repository.GetById(addedEntity.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Count_ShouldReturnCorrectCount()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        // Clear existing data and add test entities
        var existing = await repository.GetAll();
        foreach (var entity in existing)
        {
            await repository.Remove(entity);
        }

        await repository.Add(new TestEntity { Name = "Entity 1", Value = 10 });
        await repository.Add(new TestEntity { Name = "Entity 2", Value = 20 });

        // Act
        var count = await repository.Count();
        var filteredCount = await repository.Count(e => e.Value > 15);

        // Assert
        Assert.Equal(2, count);
        Assert.Equal(1, filteredCount);
    }

    [Fact]
    public async Task Any_ShouldReturnCorrectBoolean()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        // Clear existing data and add test entity
        var existing = await repository.GetAll();
        foreach (var entity in existing)
        {
            await repository.Remove(entity);
        }

        await repository.Add(new TestEntity { Name = "Entity 1", Value = 10 });

        // Act
        var any = await repository.Any();
        var anyWithFilter = await repository.Any(e => e.Value > 15);
        var anyWithFilterTrue = await repository.Any(e => e.Value == 10);

        // Assert
        Assert.True(any);
        Assert.False(anyWithFilter);
        Assert.True(anyWithFilterTrue);
    }
}
