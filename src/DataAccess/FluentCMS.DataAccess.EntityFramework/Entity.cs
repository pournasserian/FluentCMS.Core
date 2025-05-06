using FluentCMS.DataAccess.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace FluentCMS.DataAccess.EntityFramework;

public abstract class Entity : IEntity
{
    [Key]
    public Guid Id { get; set; }
}
