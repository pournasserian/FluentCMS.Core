namespace FluentCMS.DataSeeder;

internal class SeedingDiscovery(SeedingOptions options, ILogger<SeedingDiscovery> logger)
{
    public List<Type> GetSeeders()
    {
        var executablePath = Assembly.GetExecutingAssembly().Location;

        var executableFolder = Path.GetDirectoryName(executablePath) ??
            throw new InvalidOperationException("Could not determine the executable folder path.");

        // Get all DLL files in the specified directory
        var allDllFiles = Directory.GetFiles(executableFolder, "*.dll", SearchOption.TopDirectoryOnly);

        var dllFiles = allDllFiles
            .Where(dllFilePath => options.AssemblyPrefixesToScan.Any(prefix => Path.GetFileName(dllFilePath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        // Use a thread-safe collection for parallel processing
        var seeders = new ConcurrentBag<Type>();

        // Process assemblies in parallel
        Array.ForEach(dllFiles, dllPath =>
        {
            try
            {
                // Check if the assembly is already loaded before loading it
                var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName)) ??
                    Assembly.LoadFrom(dllPath);

                // Get all types first to avoid multiple calls to GetTypes()
                Type?[] allTypes = [];
                try
                {
                    allTypes = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    logger.LogWarning(ex, "ReflectionTypeLoadException while loading types from {DllPath}. Using available types.", dllPath);

                    // Log the specific loader exceptions for better diagnostics
                    if (ex.LoaderExceptions != null)
                    {
                        foreach (var loaderEx in ex.LoaderExceptions)
                        {
                            if (loaderEx != null)
                            {
                                logger.LogWarning(loaderEx, "Loader exception details for {DllPath}", dllPath);
                            }
                        }
                    }

                    // Extract valid types from the exception
                    allTypes = [.. ex.Types.Where(t => t != null)];
                }

                foreach (var type in allTypes)
                {
                    if (type != null && type.IsClass && !type.IsAbstract && typeof(ISeeder).IsAssignableFrom(type))
                    {
                        logger.LogInformation("Found data seeder: {PluginName} in assembly {AssemblyName}", type.FullName, assemblyName?.Name);
                        seeders.Add(type);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error loading assembly: {DllPath}", dllPath);
                if (!options.IgnoreExceptions)
                    throw;
            }
        });


        return [.. seeders];
    }
}
