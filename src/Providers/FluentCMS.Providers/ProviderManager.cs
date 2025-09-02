using FluentCMS.Providers.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Runtime.Loader;

namespace FluentCMS.Providers;

internal class ProviderManager : IProviderManager
{
    private readonly string[] _providerPrefixes;
    private readonly string _folderPath;
    private readonly ProviderDefinitions _providerDefinitions = new();
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, List<ProviderConfig>> _providerConfigsDict;

    public ProviderManager(string[] providerPrefixes, IConfiguration configuration)
    {
        _configuration = configuration;
        _providerPrefixes = providerPrefixes;
        _providerConfigsDict = new Dictionary<string, List<ProviderConfig>>(StringComparer.OrdinalIgnoreCase);

        var executablePath = Assembly.GetExecutingAssembly().Location;

        _folderPath = Path.GetDirectoryName(executablePath) ??
            throw new InvalidOperationException("Could not determine the executable folder path.");

        var providerConfigs = _configuration.GetSection("Providers").Get<List<ProviderConfig>>();

        if (providerConfigs != null)
        {
            foreach (var config in providerConfigs)
            {
                if (!_providerConfigsDict.TryGetValue(config.Area, out var list))
                {
                    list = [];
                    _providerConfigsDict[config.Area] = list;
                }
                list.Add(config);
            }
        }

        ScanAssemblies();
    }

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        foreach (var key in _providerConfigsDict.Keys)
        {
            var providerDef = _providerDefinitions.GetByArea(key).FirstOrDefault(d => d.Name.Equals(_providerConfigsDict[key][0].Name, StringComparison.OrdinalIgnoreCase));
            if (providerDef != null)
            {
                var configSection = _configuration.GetSection("Providers")
                    .GetChildren()
                    .FirstOrDefault(s => s.GetValue<string>("Area")?.Equals(key, StringComparison.OrdinalIgnoreCase) == true
                                      && s.GetValue<string>("Name")?.Equals(providerDef.Name, StringComparison.OrdinalIgnoreCase) == true);
                providerDef.Config = configSection ?? throw new InvalidOperationException($"Configuration section for provider '{providerDef.Name}' in area '{key}' not found.");
                // Register the provider's services
                providerDef.StartupInstance.Register(builder.Services, configSection);
            }
        }
    }

    public void Configure(IApplicationBuilder app)
    {

    }

    private static Assembly LoadIfNotLoaded(string assemblyPath)
    {
        // Normalize path
        var fullPath = Path.GetFullPath(assemblyPath);
        var fileName = Path.GetFileNameWithoutExtension(fullPath);

        // Check if already loaded
        var alreadyLoaded = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a =>
                string.Equals(a.GetName().Name, fileName, StringComparison.OrdinalIgnoreCase));

        if (alreadyLoaded != null)
        {
            return alreadyLoaded; // already loaded
        }

        // If not, load from path
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
    }

    private void ScanAssemblies()
    {
        // Get all DLL files in the specified directory
        var allDllFiles = Directory.GetFiles(_folderPath, "*.dll", SearchOption.TopDirectoryOnly);

        var dllFiles = allDllFiles
            .Where(dllFilePath => _providerPrefixes.Any(prefix => Path.GetFileName(dllFilePath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        // Process assemblies in parallel
        Array.ForEach(dllFiles, dllPath =>
        {
            try
            {
                var assembly = LoadIfNotLoaded(dllPath);

                if (assembly != null)
                {
                    // find all types implementing IProviderStartup
                    var providerStartupTypes = assembly.GetTypes()
                        .Where(t => typeof(IProviderStartup).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .ToList();

                    foreach (var startupType in providerStartupTypes)
                    {
                        var instance = (IProviderStartup?)Activator.CreateInstance(startupType);

                        if (instance != null)
                            _providerDefinitions.Add(new ProviderDefinition(instance));
                    }
                }
            }
            catch (Exception ex) when (
                ex is BadImageFormatException ||
                ex is FileLoadException ||
                ex is FileNotFoundException)
            {

            }
            catch (Exception)
            {

            }
        });
    }
}

public class ProviderDefinition(IProviderStartup startup)
{
    public ProviderDefinition(): this(null)
    {
        
    }
    public string Area { get; set; } = startup.Area;
    public string Name { get; set; }
    public string TypeName { get; set; }
    public Type ImplementationType { get; set; } = startup.Implementation;
    public Type InterfaceType { get; set; } = startup.Interface;
    public Type StartupType { get; set; } = startup.GetType();
    public IProviderStartup StartupInstance { get; set; } = startup;
    public IConfigurationSection Config { get; set; }
}

internal class ProviderDefinitions
{
    private Dictionary<string, Dictionary<string, ProviderDefinition>> _definitionsByAreaAndName = new(StringComparer.OrdinalIgnoreCase);

    public void Initialize(ProvidersConfiguration providersConfiguration) 
    {
        foreach (var area in providersConfiguration.Keys)
        {
            _definitionsByAreaAndName.TryAdd(area, new Dictionary<string, ProviderDefinition>(StringComparer.OrdinalIgnoreCase));

            var activeProviderName = providersConfiguration[area].ActiveProvider;

            foreach (var providerConfig in providersConfiguration[area].Providers)
            {
                var providerDef = new ProviderDefinition
                {
                    Area = area,
                    Name = providerConfig.Name,
                    TypeName = providerConfig.Type,
                    Config = providerConfig.Settings.HasValue ? new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string> { { "Settings", providerConfig.Settings.Value.GetRawText() } })
                        .Build()
                        .GetSection("Settings") : null
                };
            }
        }
    }

    public void Add(ProviderDefinition definition)
    {
        if (!_definitionsByArea.TryGetValue(definition.Area, out List<ProviderDefinition>? value))
        {
            value = [];
            _definitionsByArea[definition.Area] = value;
        }

        value.Add(definition);
    }

    public IEnumerable<ProviderDefinition> GetByArea(string area)
    {
        if (_definitionsByArea.TryGetValue(area, out List<ProviderDefinition>? value))
        {
            return value;
        }
        return [];
    }

    public ProviderDefinition? GetActive(string area) 
    {

    }

    public IEnumerator<ProviderDefinition> GetEnumerator()
    {
        foreach (var areaDefinitions in _definitionsByArea.Values)
        {
            foreach (var definition in areaDefinitions)
            {
                yield return definition;
            }
        }
    }
}