namespace FluentCMS.Repositories.Tests.TestEntities;

public class TestEntity : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Value { get; set; }
}
