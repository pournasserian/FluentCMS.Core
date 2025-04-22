using System.Reflection;

namespace FluentCMS.Core.Plugins.Abstractions;

public class PluginMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Assembly Assembly { get; set; } = null!;
    public Type PluginType { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
    public string AssemblyPath { get; set; } = string.Empty;
}
