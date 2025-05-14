namespace FluentCMS.ConfigOptionsProvider;

// Entity model for storing options in SQLite
public class OptionEntry : AuditableEntity
{
    public string TypeName { get; set; } = default!;
    public string Value { get; set; } = default!;
}