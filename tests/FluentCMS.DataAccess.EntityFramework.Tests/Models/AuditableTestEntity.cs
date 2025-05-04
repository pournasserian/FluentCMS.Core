using FluentCMS.DataAccess.Abstractions;

namespace FluentCMS.DataAccess.EntityFramework.Tests.Models
{
    public class AuditableTestEntity : IAuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Value { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public int Version { get; set; }
    }
}
