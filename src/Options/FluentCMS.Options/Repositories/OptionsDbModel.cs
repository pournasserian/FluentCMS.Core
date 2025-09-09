namespace FluentCMS.Options.Repositories;

public class OptionsDbModel : AuditableEntity
{
    public string Alias { get; set; } = default!;       // e.g. "identity"
    public string Type { get; set; } = default!;        // typeof(T).FullName
    public string Value { get; set; } = default!;       // serialized T
}
