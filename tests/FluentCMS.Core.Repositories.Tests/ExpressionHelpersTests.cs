namespace FluentCMS.Core.Repositories.Tests;

public class ExpressionHelpersTests
{
    [Fact]
    public void ExtractPropertyNameFromExpression_WithSimplePropertyExpression_ShouldReturnPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, string>> expression = e => e.Name;

        // Act
        string propertyName = ExpressionHelpers.ExtractPropertyNameFromExpression(expression);

        // Assert
        propertyName.Should().Be("Name");
    }

    [Fact]
    public void ExtractPropertyNameFromExpression_WithValueTypeProperty_ShouldReturnPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, int>> expression = e => e.Counter;

        // Act
        string propertyName = ExpressionHelpers.ExtractPropertyNameFromExpression(expression);

        // Assert
        propertyName.Should().Be("Counter");
    }

    [Fact]
    public void ExtractPropertyNameFromExpression_WithDateTimeProperty_ShouldReturnPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, DateTime>> expression = e => e.CreatedAt;

        // Act
        string propertyName = ExpressionHelpers.ExtractPropertyNameFromExpression(expression);

        // Assert
        propertyName.Should().Be("CreatedAt");
    }

    [Fact]
    public void ExtractPropertyNameFromExpression_WithBooleanProperty_ShouldReturnPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.IsActive;

        // Act
        string propertyName = ExpressionHelpers.ExtractPropertyNameFromExpression(expression);

        // Assert
        propertyName.Should().Be("IsActive");
    }

    [Fact]
    public void ExtractPropertyNameFromExpression_WithGuidProperty_ShouldReturnPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, Guid>> expression = e => e.Id;

        // Act
        string propertyName = ExpressionHelpers.ExtractPropertyNameFromExpression(expression);

        // Assert
        propertyName.Should().Be("Id");
    }

    [Fact]
    public void ExtractPropertyNameFromExpression_WithInvalidExpression_ShouldThrowArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Name.StartsWith("Test");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ExpressionHelpers.ExtractPropertyNameFromExpression(expression));
    }
}
