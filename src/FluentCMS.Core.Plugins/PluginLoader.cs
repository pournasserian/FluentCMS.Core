using FluentCMS.Core.Plugins.Abstractions;
using Microsoft.Extensions.Logging;
using System.Runtime.Loader;

namespace FluentCMS.Core.Plugins;

public interface IPluginLoader
{
    Task<IEnumerable<PluginMetadata>> LoadPluginsAsync(string directory);
    Task<PluginMetadata?> LoadPluginAsync(string path);
}

public class PluginLoader(ILogger<PluginLoader> logger) : IPluginLoader
{
    public Task<IEnumerable<PluginMetadata>> LoadPluginsAsync(string directory)
    {
        logger.LogInformation("Loading plugins from directory: {Directory}", directory);

        if (!Directory.Exists(directory))
        {
            logger.LogWarning("Plugin directory does not exist: {Directory}", directory);
            Directory.CreateDirectory(directory);
            return Task.FromResult(Enumerable.Empty<PluginMetadata>());
        }

        var pluginPaths = Directory.GetFiles(directory, "*.dll");
        var plugins = new List<PluginMetadata>();

        foreach (var pluginPath in pluginPaths)
        {
            var metadata = LoadPluginAsync(pluginPath).Result;
            if (metadata != null)
            {
                plugins.Add(metadata);
            }
        }

        return Task.FromResult<IEnumerable<PluginMetadata>>(plugins);
    }

    public Task<PluginMetadata?> LoadPluginAsync(string path)
    {
        try
        {
            logger.LogInformation("Loading plugin from: {Path}", path);

            // Load the assembly
            var assemblyName = Path.GetFileNameWithoutExtension(path);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

            // Find plugin types that implement IPlugin
            var pluginTypes = assembly.GetExportedTypes()
                .Where(type => typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToList();

            if (!pluginTypes.Any())
            {
                logger.LogWarning("No plugin types found in assembly: {Assembly}", assembly.FullName);
                return Task.FromResult<PluginMetadata?>(null);
            }

            // For simplicity, we'll just use the first plugin type found
            var pluginType = pluginTypes.First();
            
            // Create metadata
            var metadata = new PluginMetadata
            {
                Assembly = assembly,
                PluginType = pluginType,
                AssemblyPath = path
            };

            // Try to instantiate the plugin to get metadata
            if (Activator.CreateInstance(pluginType) is IPlugin plugin)
            {
                metadata.Name = plugin.Name;
                metadata.Version = plugin.Version;
                metadata.Description = plugin.Description;
                metadata.IsEnabled = plugin.IsEnabled;
            }
            else
            {
                // Fallback to assembly name if instantiation fails
                metadata.Name = assemblyName;
                metadata.Version = "1.0.0";
                metadata.Description = $"Plugin from {assemblyName}";
            }

            return Task.FromResult<PluginMetadata?>(metadata);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load plugin from path: {Path}", path);
            return Task.FromResult<PluginMetadata?>(null);
        }
    }
}
