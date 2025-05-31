namespace FluentCMS.Configuration;

public class OptionsEntity
{
    public Guid Id { get; set; }
    public string TypeName { get; set; } = default!;
    public string Value { get; set; } = default!;
}
