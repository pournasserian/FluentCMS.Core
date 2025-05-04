using FluentCMS.DataAccess.Abstractions;

namespace FluentCMS.DataAccess.EntityFramework;

public abstract class Entity : IEntity
{
    public Guid Id { get; set; } 
}
