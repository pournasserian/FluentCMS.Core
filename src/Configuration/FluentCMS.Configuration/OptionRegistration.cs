namespace FluentCMS.Configuration;

public class OptionRegistration
{
    public string Section { get; set; } = default!;
    public Type Type { get; set; } = default!;
    public string DefaultValue { get; set; } = default!;
}
