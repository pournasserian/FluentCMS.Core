namespace FluentCMS.Configuration.EntityFramework;

public class SettingDbModel : AuditableEntity
{
    public string Key { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? Value { get; set; }
}