namespace FluentCMS.Core.Repositories.Tests;

// Simple entity class for testing repository operations
public class TestEntity : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Counter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
