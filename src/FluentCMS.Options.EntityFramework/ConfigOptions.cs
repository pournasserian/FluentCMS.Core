namespace FluentCMS.Options.EntityFramework;

public class ConfigOptions : AuditableEntity
{
    public string TypeName { get; set; } = default!;
    public string Value { get; set; } = default!;
}
