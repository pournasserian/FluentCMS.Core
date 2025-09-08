namespace FluentCMS.Configuration.EntityFramework;

public class Setting<T> : AuditableEntity
{
    public string Key { get; set; } = default!;
    public T? Value { get; set; }
}
