namespace FluentCMS.Core.Plugins;

public class PluginManager : IPluginManager
{
    private readonly IEnumerable<IPluginMetadata> _pluginsMetaData;
    private readonly List<IPlugin> _pluginInstances = [];

    public PluginManager()
    {
        var executablePath = Assembly.GetExecutingAssembly().Location;
        
        var executanbleFolder = Path.GetDirectoryName(executablePath) ??
            throw new InvalidOperationException("Could not determine the executable folder path.");

        _pluginsMetaData = ScanAssemblies(executanbleFolder);
    }

    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        foreach (var pluginMetaData in _pluginsMetaData)
        {
            try
            {
                // Get the plugin type which implements IPlugin
                var pluginType = pluginMetaData.Type ??
                    throw new InvalidOperationException($"Plugin type not found for {pluginMetaData.Name}");

                // Create an instance of the plugin
                var plugin = Activator.CreateInstance(pluginType) as IPlugin ??
                    throw new InvalidOperationException($"Failed to create instance of plugin {pluginMetaData.Name}");

                services.AddSingleton(typeof(IPlugin), plugin);
                _pluginInstances.Add(plugin);

                // Call the ConfigureServices method on the plugin
                plugin.ConfigureServices(services);

            }
            catch (Exception)
            {
            }
        }
        return services;
    }

    public IApplicationBuilder Configure(IApplicationBuilder app)
    {
        foreach (var pluginInstance in _pluginInstances)
        {
            // Call the Configure method on the plugin
            pluginInstance.Configure(app);
        }
        return app;
    }

    public IEnumerable<IPlugin> GetPlugins()
    {
        return _pluginInstances;
    }

    public IEnumerable<IPluginMetadata> GetPluginMetadata()
    {
        return _pluginsMetaData;
    }

    private static IEnumerable<IPluginMetadata> ScanAssemblies(string folderPath)
    {
        // Get all DLL files in the specified directory
        var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.TopDirectoryOnly)
            .Where(file => !Path.GetFileName(file).StartsWith("System.") &&
                          !Path.GetFileName(file).StartsWith("Microsoft."))
            .ToArray();

        // Use a thread-safe collection for parallel processing
        var pluginsMetaData = new ConcurrentBag<IPluginMetadata>();

        // Process assemblies in parallel
        Array.ForEach(dllFiles, dllPath =>
        {
            try
            {
                // Load the assembly
                var assembly = Assembly.LoadFrom(dllPath);
                var assemblyName = assembly.GetName();

                // Get all types first to avoid multiple calls to GetTypes()
                Type?[] allTypes = [];
                try
                {
                    allTypes = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Extract valid types from the exception
                    allTypes = [.. ex.Types.Where(t => t != null)];
                }

                foreach (var type in allTypes)
                {
                    if (type != null && type.IsClass && !type.IsAbstract && typeof(IPlugin).IsAssignableFrom(type))
                    {
                        var descriptionAttribute = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();


                        var pluginMetaData = new PluginMetadata
                        {
                            Name = assemblyName?.Name ?? type.Name,
                            Description = descriptionAttribute?.Description ?? "Not specified",
                            Version = assemblyName?.Version?.ToString() ?? string.Empty,
                            FileName = Path.GetFileName(dllPath),
                            Assembly = assembly,
                            Type = type
                        };

                        pluginsMetaData.Add(pluginMetaData);
                    }
                }
            }
            catch (Exception ex) when (
                ex is BadImageFormatException ||
                ex is FileLoadException ||
                ex is FileNotFoundException)
            {
                // Skip assemblies that cannot be loaded or are not .NET assemblies
            }
        });

        return pluginsMetaData.AsEnumerable();
    }
}
