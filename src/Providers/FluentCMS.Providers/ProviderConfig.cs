using System.Text.Json;

namespace FluentCMS.Providers;

public sealed class ProviderConfig
{
    public string Area { get; set; } = default!;
    public string Name { get; set; } = default!;
    public JsonElement? Settings { get; set; }     // provider-specific JSON
}

public sealed class ProvidersConfiguration: Dictionary<string, ProviderAreaConfiguration>
{
    public List<ProviderConfig> Providers { get; set; } = [];
}

public sealed class ProviderAreaConfiguration
{
    public string ActiveProvider { get; set; } = default!;
    public List<ProviderConfiguration> Providers { get; set; } = [];
}

public sealed class ProviderConfiguration
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public JsonElement? Settings { get; set; }
}