using FluentAssertions;
using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.Core.Repositories.LiteDB;
using System.Linq.Expressions;
using Xunit;

namespace FluentCMS.Core.Repositories.Tests;

public class LiteDBRepositoryTests : IClassFixture<LiteDBContextFixture>
{
    private readonly LiteDBContextFixture _fixture;
    private readonly LiteDBRepository<TestEntity> _repository;

    public LiteDBRepositoryTests(LiteDBContextFixture fixture)
    {
        _fixture = fixture;
        _repository = new LiteDBRepository<TestEntity>(_fixture.Context);
    }

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnEntity_WhenEntityExists()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            Counter = 1
        };
        await _repository.Add(entity);

        // Act
        var result = await _repository.GetById(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be(entity.Name);
        result.Description.Should().Be(entity.Description);
        result.Counter.Should().Be(entity.Counter);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetById(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetById_ShouldThrowOperationCancelledException_WhenCancellationRequested()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.GetById(Guid.NewGuid(), cancellationTokenSource.Token));
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ShouldReturnAllEntities()
    {
        // Arrange
        await ClearCollection();

        var entities = new List<TestEntity>
        {
            new() { Name = "Entity 1", Counter = 1 },
            new() { Name = "Entity 2", Counter = 2 },
            new() { Name = "Entity 3", Counter = 3 }
        };

        foreach (var entity in entities)
        {
            await _repository.Add(entity);
        }

        // Act
        var results = await _repository.GetAll();

        // Assert
        results.Should().HaveCount(entities.Count);
        results.Select(e => e.Id).Should().Contain(entities.Select(e => e.Id));
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyCollection_WhenNoEntitiesExist()
    {
        // Arrange
        await ClearCollection();

        // Act
        var results = await _repository.GetAll();

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task Query_ShouldReturnFilteredEntities_WhenFilterApplied()
    {
        // Arrange
        await ClearCollection();

        var entities = new List<TestEntity>
        {
            new() { Name = "Active Entity", IsActive = true },
            new() { Name = "Inactive Entity", IsActive = false },
            new() { Name = "Another Active Entity", IsActive = true }
        };

        foreach (var entity in entities)
        {
            await _repository.Add(entity);
        }

        // Act
        Expression<Func<TestEntity, bool>> filter = e => e.IsActive;
        var results = await _repository.Query(filter);

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task Query_ShouldReturnPaginatedResults_WhenPaginationOptionsProvided()
    {
        // Arrange
        await ClearCollection();
        await EnsureTestData();

        // Act
        var paginationOptions = new PaginationOptions { PageNumber = 3, PageSize = 3 };
        var results = await _repository.Query(default, paginationOptions);

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task Query_ShouldApplyFilterAndPagination_WhenBothProvided()
    {
        // Arrange
        await ClearCollection();
        await EnsureTestData();

        // Act
        Expression<Func<TestEntity, bool>> filter = e => e.IsActive;
        var paginationOptions = new PaginationOptions { PageNumber = 2, PageSize = 2 };
        var results = await _repository.Query(filter, paginationOptions);

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task Query_WithQueryOptions_ShouldApplyFilterAndPagination()
    {
        // Arrange
        await ClearCollection();
        await EnsureTestData();

        // Act
        var queryOptions = new QueryOptions<TestEntity>
        {
            Filter = e => e.IsActive,
            Pagination = new PaginationOptions { PageNumber = 2, PageSize = 2 }
        };

        var results = await _repository.Query(queryOptions);

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.IsActive).Should().BeTrue();
    }

    #endregion

    #region Count Tests

    [Fact]
    public async Task Count_ShouldReturnTotalNumberOfEntities_WhenNoFilterProvided()
    {
        // Arrange
        await ClearCollection();

        var entities = new List<TestEntity>
        {
            new() { Name = "Entity 1" },
            new() { Name = "Entity 2" },
            new() { Name = "Entity 3" }
        };

        foreach (var entity in entities)
        {
            await _repository.Add(entity);
        }

        // Act
        var count = await _repository.Count();

        // Assert
        count.Should().Be(entities.Count);
    }

    [Fact]
    public async Task Count_ShouldReturnFilteredCount_WhenFilterProvided()
    {
        // Arrange
        await ClearCollection();

        var entities = new List<TestEntity>
        {
            new() { Name = "Active Entity", IsActive = true },
            new() { Name = "Inactive Entity", IsActive = false },
            new() { Name = "Another Active Entity", IsActive = true }
        };

        foreach (var entity in entities)
        {
            await _repository.Add(entity);
        }

        // Act
        Expression<Func<TestEntity, bool>> filter = e => e.IsActive;
        var count = await _repository.Count(filter);

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region Add Tests

    [Fact]
    public async Task Add_ShouldInsertEntity_AndReturnEntityWithGeneratedId()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "New Entity",
            Description = "Description for new entity",
            Counter = 42
        };

        // Act
        var result = await _repository.Add(entity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);

        // Verify the entity was saved
        var savedEntity = await _repository.GetById(result.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Name.Should().Be(entity.Name);
        savedEntity.Counter.Should().Be(entity.Counter);
    }

    [Fact]
    public async Task Add_ShouldPreserveProvidedId_WhenIdIsNotEmpty()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestEntity
        {
            Id = id,
            Name = "Entity with Specific ID"
        };

        // Act
        var result = await _repository.Add(entity);

        // Assert
        result.Id.Should().Be(id);

        // Verify the entity was saved with the provided id
        var savedEntity = await _repository.GetById(id);
        savedEntity.Should().NotBeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldModifyExistingEntity()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Original Name",
            Description = "Original Description",
            Counter = 1
        };

        var addedEntity = await _repository.Add(entity);

        // Modify the entity
        addedEntity.Name = "Updated Name";
        addedEntity.Description = "Updated Description";
        addedEntity.Counter = 100;

        // Act
        var result = await _repository.Update(addedEntity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(addedEntity.Id);
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.Counter.Should().Be(100);

        // Verify the changes were saved
        var updatedEntity = await _repository.GetById(addedEntity.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Name.Should().Be("Updated Name");
        updatedEntity.Description.Should().Be("Updated Description");
        updatedEntity.Counter.Should().Be(100);
    }

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenEntityDoesNotExist()
    {
        // Arrange
        var nonExistentEntity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Non-existent Entity"
        };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _repository.Update(nonExistentEntity));
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldDeleteEntity_WhenEntityExists()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Entity to Delete"
        };

        var addedEntity = await _repository.Add(entity);

        // Act
        await _repository.Remove(addedEntity.Id);

        // Assert
        var deletedEntity = await _repository.GetById(addedEntity.Id);
        deletedEntity.Should().BeNull();
    }

    [Fact]
    public async Task Remove_ShouldThrowEntityNotFoundException_WhenEntityDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _repository.Remove(nonExistentId));
    }

    #endregion

    #region Helper Methods

    private async Task EnsureTestData() 
    {
        var entities = new List<TestEntity>();
        for (int i = 1; i <= 10; i++)
        {
            entities.Add(new TestEntity
            {
                Name = $"Entity {i}",
                Counter = i,
                IsActive = i % 2 == 0 // Even-numbered entities are active
            });
        }
        foreach (var entity in entities)
        {
            await _repository.Add(entity);
        }
    }

    private async Task ClearCollection()
    {
        // Get all existing entities
        var entities = await _repository.GetAll();

        // Remove each entity
        foreach (var entity in entities)
        {
            try
            {
                await _repository.Remove(entity.Id);
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    #endregion
}
