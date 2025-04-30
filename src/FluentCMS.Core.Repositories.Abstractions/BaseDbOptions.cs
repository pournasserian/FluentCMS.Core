using System.ComponentModel.DataAnnotations;

namespace FluentCMS.Core.Repositories.Abstractions;

public abstract class BaseDbOptions
{
    [Required(ErrorMessage = "ConnectionString is required for DbOptions.")]
    public string ConnectionString { get; set; } = default!;
}
