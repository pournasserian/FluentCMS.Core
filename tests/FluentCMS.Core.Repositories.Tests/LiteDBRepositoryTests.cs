using FluentAssertions;
using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.Core.Repositories.LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
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
        var logger = new NullLogger<LiteDBRepository<TestEntity>>();
        _repository = new LiteDBRepository<TestEntity>(_fixture.Context, logger);
    }

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnEntity_WhenEntityExists()
    {
        // Arrange
        await ClearCollection();
        var entity = await AddTestData();

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
    public async Task GetById_ShouldThrowEntityNotFoundException_WhenEntityDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _repository.GetById(nonExistentId));
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
        var entities = await AddRangeTestData(5);

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

    [Fact]
    public async Task GetAll_ShouldThrowOperationCancelledException_WhenCancellationRequested()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.GetAll(cancellationTokenSource.Token));
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task Query_ShouldReturnFilteredEntities_WhenFilterApplied()
    {
        // Arrange
        await ClearCollection();
        var entities = await AddRangeTestData(3);

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
        await AddRangeTestData(10);

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
        await AddRangeTestData(10);

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
        await AddRangeTestData(10);

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

    [Fact]
    public async Task Query_WithSorting_ShouldReturnSortedEntities_WhenSortingProvided()
    {
        // Arrange
        await ClearCollection();
        var entities = await AddRangeTestData(3);

        // Act
        var queryOptions = new QueryOptions<TestEntity>
        {
            Sorting = new List<SortOption<TestEntity>>
            {
                new SortOption<TestEntity>(e => e.Counter, SortDirection.Ascending)
            }
        };

        var results = await _repository.Query(queryOptions);

        // Assert
        results.Select(e => e.Counter).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Query_WithDescendingSorting_ShouldReturnDescendingSortedEntities()
    {
        // Arrange
        await ClearCollection();
        await AddRangeTestData(5);

        // Act
        var queryOptions = new QueryOptions<TestEntity>
        {
            Sorting = new List<SortOption<TestEntity>>
            {
                new SortOption<TestEntity>(e => e.Counter, SortDirection.Descending)
            }
        };

        var results = await _repository.Query(queryOptions);

        // Assert
        results.Select(e => e.Counter).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Query_WithMultipleSortOptions_ShouldApplyAllSortCriteria()
    {
        // Arrange
        await ClearCollection();
        
        // Add entities with different IsActive values but some with the same Counter
        var entity1 = new TestEntity { Name = "Entity 1", Counter = 5, IsActive = true };
        var entity2 = new TestEntity { Name = "Entity 2", Counter = 5, IsActive = false };
        var entity3 = new TestEntity { Name = "Entity 3", Counter = 10, IsActive = true };
        
        await _repository.Add(entity1);
        await _repository.Add(entity2);
        await _repository.Add(entity3);

        // Act
        // Sort by Counter (ascending), then by IsActive (descending)
        var queryOptions = new QueryOptions<TestEntity>
        {
            Sorting = new List<SortOption<TestEntity>>
            {
                new SortOption<TestEntity>(e => e.Counter, SortDirection.Ascending),
                new SortOption<TestEntity>(e => e.IsActive, SortDirection.Descending)
            }
        };

        var results = await _repository.Query(queryOptions);
        var resultsList = results.ToList();

        // Assert
        // First two entries should have Counter = 5, with the active one first
        resultsList.Count.Should().Be(3);
        resultsList[0].Counter.Should().Be(5);
        resultsList[1].Counter.Should().Be(5);
        
        // For the entries with Counter = 5, IsActive = true should come first
        if (resultsList[0].Counter == 5 && resultsList[1].Counter == 5)
        {
            resultsList[0].IsActive.Should().BeTrue();
            resultsList[1].IsActive.Should().BeFalse();
        }
        
        // Last entry should have Counter = 10
        resultsList[2].Counter.Should().Be(10);
    }

    [Fact]
    public async Task Query_WithEmptyResults_ShouldReturnEmptyCollection()
    {
        // Arrange
        await ClearCollection();
        await AddRangeTestData(3);

        // Act
        Expression<Func<TestEntity, bool>> filter = e => e.Counter > 100; // No entities have Counter > 100
        var results = await _repository.Query(filter);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_WithPaginationBeyondResultSet_ShouldReturnEmptyCollection()
    {
        // Arrange
        await ClearCollection();
        await AddRangeTestData(5); // Adds 6 items (count + 1)

        // Act
        var paginationOptions = new PaginationOptions { PageNumber = 10, PageSize = 10 }; // Page beyond available entities
        var results = await _repository.Query(default, paginationOptions);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_ShouldThrowOperationCancelledException_WhenCancellationRequested()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.Query(default, default, default, cancellationTokenSource.Token));
    }

    #endregion

    #region Count Tests

    [Fact]
    public async Task Count_ShouldReturnTotalNumberOfEntities_WhenNoFilterProvided()
    {
        // Arrange
        await ClearCollection();

        var entities = await AddRangeTestData(10);

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
        var entities = await AddRangeTestData(3);

        // Act
        Expression<Func<TestEntity, bool>> filter = e => e.IsActive;
        var count = await _repository.Count(filter);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task Count_ShouldReturnZero_WhenNoEntitiesMatchFilter()
    {
        // Arrange
        await ClearCollection();
        await AddRangeTestData(5);

        // Act
        Expression<Func<TestEntity, bool>> filter = e => e.Counter > 100; // No entities have Counter > 100
        var count = await _repository.Count(filter);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task Count_ShouldThrowOperationCancelledException_WhenCancellationRequested()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.Count(default, cancellationTokenSource.Token));
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
        savedEntity.Name.Should().Be(entity.Name);
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

    [Fact]
    public async Task Add_ShouldThrowOperationCancelledException_WhenCancellationRequested()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.Add(entity, cancellationTokenSource.Token));
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

    [Fact]
    public async Task Update_ShouldThrowOperationCancelledException_WhenCancellationRequested()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test Entity" };
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.Update(entity, cancellationTokenSource.Token));
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
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _repository.GetById(addedEntity.Id));
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

    [Fact]
    public async Task Remove_ShouldThrowOperationCancelledException_WhenCancellationRequested()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.Remove(id, cancellationTokenSource.Token));
    }

    #endregion

    #region Helper Methods

    private async Task<TestEntity> AddTestData()
    {
        var i = 1;
        var entity = new TestEntity
        {
            Name = $"Entity {i}",
            Description = $"Entity Description {i}",
            Counter = i,
            IsActive = i % 2 == 0 // Even-numbered entities are active
        };

        await _repository.Add(entity);
        return entity;
    }

    private async Task<List<TestEntity>> AddRangeTestData(int count)
    {
        var entities = new List<TestEntity>();
        for (int i = 1; i <= count + 1; i++)
        {
            entities.Add(new TestEntity
            {
                Name = $"Entity {i}",
                Description = $"Entity Description {i}",
                Counter = i,
                IsActive = i % 2 == 0 // Even-numbered entities are active
            });
        }
        foreach (var entity in entities)
        {
            await _repository.Add(entity);
        }
        return entities;
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
            catch (EntityNotFoundException)
            {
                // Entity may have been deleted already
            }
            catch (Exception)
            {
                // Ignore other errors during cleanup
            }
        }
    }

    #endregion
}
