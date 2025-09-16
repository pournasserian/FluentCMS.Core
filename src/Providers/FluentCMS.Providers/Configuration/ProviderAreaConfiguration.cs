namespace FluentCMS.Providers.Configuration;

public class ProviderConfigurationRoot : Dictionary<string, List<ProviderAreaConfiguration>>
{
}

public class ProviderAreaConfiguration
{
    public string Name { get; set; } = string.Empty;
    public bool? Active { get; set; }
    public string Module { get; set; } = string.Empty;
    public Dictionary<string, object?>? Options { get; set; } // stays as dictionary
}