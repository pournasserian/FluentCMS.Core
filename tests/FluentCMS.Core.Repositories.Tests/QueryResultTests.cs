namespace FluentCMS.Core.Repositories.Tests;

public class QueryResultTests
{
    [Fact]
    public void QueryResult_DefaultConstructor_InitializesEmptyItems()
    {
        // Act
        var result = new QueryResult<TestEntity>();
        
        // Assert
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
    
    [Fact]
    public void QueryResult_WithItems_ShouldStoreItemsCorrectly()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new TestEntity { Name = "Test 1" },
            new TestEntity { Name = "Test 2" }
        };
        
        // Act
        var result = new QueryResult<TestEntity>
        {
            Items = entities,
            TotalCount = 10 // Simulating a larger dataset with pagination
        };
        
        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().BeEquivalentTo(entities);
        result.TotalCount.Should().Be(10);
    }
    
    [Fact]
    public void QueryResult_WithZeroItems_ShouldHaveCorrectTotalCount()
    {
        // Arrange & Act
        var result = new QueryResult<TestEntity>
        {
            Items = new List<TestEntity>(),
            TotalCount = 5 // No items on current page, but 5 total in dataset
        };
        
        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(5);
    }
    
    [Theory]
    [InlineData(0, 10, 0)]   // Empty result
    [InlineData(5, 10, 1)]   // 5 items, page size 10 = 1 page
    [InlineData(10, 10, 1)]  // 10 items, page size 10 = 1 page
    [InlineData(11, 10, 2)]  // 11 items, page size 10 = 2 pages
    [InlineData(20, 10, 2)]  // 20 items, page size 10 = 2 pages
    [InlineData(21, 10, 3)]  // 21 items, page size 10 = 3 pages
    public void CanCalculateCorrectPageCount(int totalCount, int pageSize, int expectedPages)
    {
        // Arrange
        var result = new QueryResult<TestEntity> { TotalCount = totalCount };
        
        // Act
        var pageCount = (int)Math.Ceiling((double)result.TotalCount / pageSize);
        
        // Assert
        pageCount.Should().Be(expectedPages);
    }
}
