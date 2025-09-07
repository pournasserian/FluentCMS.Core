namespace FluentCMS.Configuration.EntityFramework;

public class ConfigurationSetting : AuditableEntity
{
    public string Key { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string? Value { get; set; }
}