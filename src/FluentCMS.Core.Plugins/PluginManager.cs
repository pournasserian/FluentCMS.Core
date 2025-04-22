using FluentCMS.Core.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Core.Plugins;

public interface IPluginManager
{
    Task InitializeAsync(IServiceCollection services);
    Task StartAllAsync();
    Task StopAllAsync();
    IEnumerable<IPlugin> GetPlugins();
    IPlugin? GetPlugin(string name);
    Task<bool> EnablePluginAsync(string name);
    Task<bool> DisablePluginAsync(string name);
    Task<bool> UninstallPluginAsync(string name);
    Task<PluginMetadata?> InstallPluginAsync(Stream pluginStream, string fileName);
}

public class PluginManager(ILogger<PluginManager> logger, IPluginLoader pluginLoader, string pluginDirectory) : IPluginManager
{
    private readonly List<IPlugin> _plugins = [];

    public async Task InitializeAsync(IServiceCollection services)
    {
        logger.LogInformation("Initializing plugin manager");

        // Clear existing plugins
        _plugins.Clear();

        // Load plugins
        var pluginMetadataList = await pluginLoader.LoadPluginsAsync(pluginDirectory);

        foreach (var metadata in pluginMetadataList)
        {
            try
            {
                var plugin = CreatePluginInstance(metadata);

                if (plugin != null)
                {
                    // Initialize the plugin
                    var initialized = plugin.Initialize(services);

                    if (initialized)
                    {
                        _plugins.Add(plugin);
                        logger.LogInformation("Plugin initialized: {Name} {Version}", plugin.Name, plugin.Version);
                    }
                    else
                    {
                        logger.LogWarning("Plugin failed to initialize: {Name}", metadata.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize plugin: {Name}", metadata.Name);
            }
        }

        logger.LogInformation("Plugin initialization completed. {Count} plugins loaded", _plugins.Count);
    }

    public async Task StartAllAsync()
    {
        logger.LogInformation("Starting all plugins");

        foreach (var plugin in _plugins.Where(p => p.IsEnabled))
        {
            try
            {
                var started = await plugin.Start();
                if (started)
                {
                    logger.LogInformation("Plugin started: {Name}", plugin.Name);
                }
                else
                {
                    logger.LogWarning("Plugin failed to start: {Name}", plugin.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting plugin: {Name}", plugin.Name);
            }
        }
    }

    public async Task StopAllAsync()
    {
        logger.LogInformation("Stopping all plugins");

        foreach (var plugin in _plugins.Where(p => p.IsEnabled))
        {
            try
            {
                var stopped = await plugin.Stop();
                if (stopped)
                {
                    logger.LogInformation("Plugin stopped: {Name}", plugin.Name);
                }
                else
                {
                    logger.LogWarning("Plugin failed to stop: {Name}", plugin.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping plugin: {Name}", plugin.Name);
            }
        }
    }

    public IEnumerable<IPlugin> GetPlugins()
    {
        return _plugins;
    }

    public IPlugin? GetPlugin(string name)
    {
        return _plugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> EnablePluginAsync(string name)
    {
        var plugin = GetPlugin(name);

        if (plugin == null)
        {
            logger.LogWarning("Plugin not found: {Name}", name);
            return false;
        }

        if (plugin.IsEnabled)
        {
            logger.LogInformation("Plugin already enabled: {Name}", name);
            return true;
        }

        plugin.IsEnabled = true;

        // Start the plugin since it's now enabled
        try
        {
            var started = await plugin.Start();
            if (started)
            {
                logger.LogInformation("Plugin enabled and started: {Name}", name);
                return true;
            }
            else
            {
                logger.LogWarning("Plugin enabled but failed to start: {Name}", name);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting plugin after enabling: {Name}", name);
            return false;
        }
    }

    public async Task<bool> DisablePluginAsync(string name)
    {
        var plugin = GetPlugin(name);

        if (plugin == null)
        {
            logger.LogWarning("Plugin not found: {Name}", name);
            return false;
        }

        if (!plugin.IsEnabled)
        {
            logger.LogInformation("Plugin already disabled: {Name}", name);
            return true;
        }

        // Stop the plugin before disabling
        try
        {
            var stopped = await plugin.Stop();
            plugin.IsEnabled = false;

            if (stopped)
            {
                logger.LogInformation("Plugin stopped and disabled: {Name}", name);
            }
            else
            {
                logger.LogWarning("Plugin disabled but failed to stop gracefully: {Name}", name);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping plugin before disabling: {Name}", name);
            plugin.IsEnabled = false; // Still disable it even if stopping fails
            return false;
        }
    }

    public async Task<bool> UninstallPluginAsync(string name)
    {
        var plugin = GetPlugin(name);

        if (plugin == null)
        {
            logger.LogWarning("Plugin not found: {Name}", name);
            return false;
        }

        // First, make sure the plugin is stopped and disabled
        if (plugin.IsEnabled)
        {
            await DisablePluginAsync(name);
        }

        // Find the metadata to get the file path
        var pluginMetadataList = await pluginLoader.LoadPluginsAsync(pluginDirectory);
        var metadata = pluginMetadataList.FirstOrDefault(m =>
            m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (metadata == null || string.IsNullOrEmpty(metadata.AssemblyPath))
        {
            logger.LogWarning("Plugin file not found for: {Name}", name);
            return false;
        }

        try
        {
            // Remove from the loaded plugins list
            _plugins.Remove(plugin);

            // Delete the file
            File.Delete(metadata.AssemblyPath);
            logger.LogInformation("Plugin uninstalled: {Name}", name);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uninstalling plugin: {Name}", name);
            return false;
        }
    }

    public async Task<PluginMetadata?> InstallPluginAsync(Stream pluginStream, string fileName)
    {
        var filePath = Path.Combine(pluginDirectory, fileName);

        try
        {
            // Ensure plugin directory exists
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }

            // Write the plugin file
            using (var fileStream = File.Create(filePath))
            {
                await pluginStream.CopyToAsync(fileStream);
            }

            // Load the plugin
            var metadata = await pluginLoader.LoadPluginAsync(filePath);

            if (metadata == null)
            {
                logger.LogWarning("Failed to load installed plugin: {FileName}", fileName);
                // Clean up the file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return null;
            }

            logger.LogInformation("Plugin installed: {Name} {Version}", metadata.Name, metadata.Version);
            return metadata;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error installing plugin: {FileName}", fileName);
            // Clean up the file if it exists
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return null;
        }
    }

    private IPlugin? CreatePluginInstance(PluginMetadata metadata)
    {
        try
        {
            var instance = Activator.CreateInstance(metadata.PluginType) as IPlugin;

            if (instance == null)
            {
                logger.LogWarning("Failed to create plugin instance: {Name}", metadata.Name);
                return null;
            }

            // Set the enabled state based on metadata
            instance.IsEnabled = metadata.IsEnabled;

            return instance;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating plugin instance: {Name}", metadata.Name);
            return null;
        }
    }
}
