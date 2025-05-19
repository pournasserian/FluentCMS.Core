namespace FluentCMS.Providers;

public class ProviderScanner(ILogger<ProviderScanner>? logger, string[] namespacePrefixes)
{
    public IEnumerable<ProviderInterfaceMetaData> FindProviders()
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


        var providerInterfaces = new Dictionary<string, ProviderInterfaceMetaData>(); // key is the interface full name, value is interface type

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
                        // Find all types that implemets IProvider interface and are not abstract
                        var providerTypes = assembly.GetTypes()
                            .Where(t =>
                                t.IsClass &&
                                !t.IsAbstract &&
                                t.GetCustomAttributesData().Any(attr => attr.AttributeType.GetInterface(typeof(IProviderMetadataAttributeAnchor).FullName!) != null))
                            .ToList();

                        foreach (var providerType in providerTypes)
                        {
                            // Get instance of the provider attribute.
                            var providerAttrData = providerType.GetCustomAttributesData().
                                Where(attr => attr.AttributeType.GetInterface(typeof(IProviderMetadataAttributeAnchor).FullName!) != null).
                                Single();

                            
                            var interfaceType = providerAttrData.AttributeType.GetGenericArguments()[0];
                            var dictKey = interfaceType.FullName!;

                            var providerMetaData = new ProviderMetaData(providerType, providerAttrData);

                            providerInterfaces.TryAdd(dictKey, new ProviderInterfaceMetaData(interfaceType));

                            // Check id there is any existing implementation which is default.
                            if (providerInterfaces[dictKey].Implementations.Any(x => x.IsDefault))
                            {
                                // If there is a default implementation, add the new one as non-default
                                // Log warning about this
                                logger?.LogWarning("Provider {providerName} is not default because there is already a default provider for {interfaceName}.", providerMetaData.Name, dictKey);
                                providerMetaData.IsDefault = false;
                            }
                            providerInterfaces[dictKey].Implementations.Add(providerMetaData);
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

public class ProviderInterfaceMetaData(Type type)
{
    public string TypeName { get; set; } = type.Name;
    public string TypeFullName { get; set; } = type.FullName ?? throw new InvalidOperationException("Type full name is null.");
    public string Assembly { get; set; } = type.Assembly.FullName ?? throw new InvalidOperationException("Assembly full name is null.");
    public string AssemblyFile { get; set; } = Path.GetFileName(type.Assembly.Location) ?? throw new InvalidOperationException("Assembly location is null.");
    public List<ProviderMetaData> Implementations { get; } = [];
}

public class ProviderMetaData(Type type, CustomAttributeData customAttributeData)
{
    public string Category { get; set; } = customAttributeData.ConstructorArguments[0].Value as string ?? throw new InvalidOperationException("Category is null.");
    public string Name { get; set; } = customAttributeData.ConstructorArguments[1].Value as string ?? throw new InvalidOperationException("Name is null.");
    public string Description { get; set; } = customAttributeData.ConstructorArguments[2].Value as string ?? throw new InvalidOperationException("Description is null.");
    public string Version { get; set; } = customAttributeData.ConstructorArguments[3].Value as string ?? throw new InvalidOperationException("Version is null.");
    public bool IsDefault { get; set; } = (bool)customAttributeData.ConstructorArguments[4].Value!;

    public string TypeName { get; set; } = type.Name;
    public string TypeFullName { get; set; } = type.FullName ?? throw new InvalidOperationException("Type full name is null.");
    public string AssemblyFile { get; set; } = Path.GetFileName(type.Assembly.Location) ?? throw new InvalidOperationException("Assembly location is null.");
    public string Assembly { get; set; } = type.Assembly.FullName ?? throw new InvalidOperationException("Assembly full name is null.");
}