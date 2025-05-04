using FluentCMS.DataAccess.Abstractions;

namespace FluentCMS.DataAccess.EntityFramework.Tests.Models
{
    public class TestEntity : IAuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
