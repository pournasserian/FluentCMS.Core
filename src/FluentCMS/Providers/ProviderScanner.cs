using System.Reflection;

namespace FluentCMS.Providers;

public class ProviderScanner(ILogger<ProviderScanner>? logger, string[] namespacePrefixes)
{
    public IEnumerable<ProviderInterfaceInfo> FindAssembliesImplementingInterface()
    {
        var executablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        var executanbleFolder = Path.GetDirectoryName(executablePath) ??
            throw new InvalidOperationException("Could not determine the executable folder path.");

        // Get all DLL files in the directory
        var assemblyFiles = Directory.GetFiles(executablePath, "*.dll", SearchOption.AllDirectories).ToList();
        var systemPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        assemblyFiles.AddRange([
            typeof(object).Assembly.Location,
            Path.Combine(systemPath, "netstandard.dll"),
            Path.Combine(systemPath, "System.Runtime.dll")
        ]);


        var providerInterfaces = new Dictionary<string, ProviderInterfaceInfo>(); // key is the interface full name, value is interface type

        // Create resolver for dependencies
        var resolver = new PathAssemblyResolver(assemblyFiles);

        // Create MetadataLoadContext which won't execute code
        using (var context = new MetadataLoadContext(resolver))
        {
            foreach (var assemblyFile in assemblyFiles.Where(x => namespacePrefixes.Any(prefix => Path.GetFileName(x).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
            {
                try
                {
                    // Load assembly metadata without executing code
                    var assembly = context.LoadFromAssemblyPath(assemblyFile);

                    try
                    {
                        // Find all types that hass ProviderAttribute
                        var providerTypes = assembly.GetTypes()
                            .Where(t => 
                                t.IsClass && 
                                !t.IsAbstract &&
                                t.GetCustomAttributesData().Any(attr => 
                                    attr.AttributeType.FullName == typeof(ProviderAttribute).FullName))
                            .ToList();

                        foreach (var providerType in providerTypes)
                        {
                            // Get the highest level interface for the implementation
                            var interfaces = providerType.GetInterfaces();

                            // Check if there is only one provider interface
                            if (interfaces.Length != 1)
                            {
                                // Log error and continue to next type
                                logger?.LogError("Type {FullName} has multiple provider interfaces. Skipping.", providerType.FullName);
                                continue;
                            }
                            var interfaceType = interfaces[0];
                            var interfaceFullNmae = interfaceType.FullName!;

                            if (!providerInterfaces.ContainsKey(interfaceFullNmae))
                                providerInterfaces.Add(interfaceFullNmae, new ProviderInterfaceInfo(interfaceType));

                            providerInterfaces[interfaceFullNmae].Implementations.Add(new ProviderInfo(providerType));
                        }
                    }
                    catch (Exception)
                    {
                        // Log error and continue to next assembly
                        logger?.LogError("Could not load types from assembly {assemblyFile}.", assemblyFile);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not process assembly {assemblyFile}: {ex.Message}");
                }
            }
        }

        return providerInterfaces.Values;
    }
}

public class ProviderInterfaceInfo(Type type)
{
    public string Name { get; set; } = type.Name;
    public string FullTypeName { get; set; } = type.FullName ?? throw new InvalidOperationException("Type full name is null.");
    public string Assembly { get; set; } = type.Assembly.FullName ?? throw new InvalidOperationException("Assembly full name is null.");
    public string AssemblyPath { get; set; } = type.Assembly.Location ?? throw new InvalidOperationException("Assembly location is null.");
    public List<ProviderInfo> Implementations { get; set; } = [];
}

public class ProviderInfo(Type type)
{
    public string Name { get; set; } = type.Name;
    public string FullTypeName { get; set; } = type.FullName ?? throw new InvalidOperationException("Type full name is null.");
    public string AssemblyPath { get; set; } = type.Assembly.Location ?? throw new InvalidOperationException("Assembly location is null.");
    public string Assembly { get; set; } = type.Assembly.FullName ?? throw new InvalidOperationException("Assembly full name is null.");
}