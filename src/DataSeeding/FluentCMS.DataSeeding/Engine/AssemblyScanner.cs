using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FluentCMS.DataSeeding.Engine;

/// <summary>
/// Scans assemblies for types implementing specific interfaces using wildcard patterns.
/// Provides the auto-discovery functionality for seeders and schema validators.
/// </summary>
public class AssemblyScanner
{
    private readonly Dictionary<string, Assembly[]> _assemblyCache = new();
    private readonly object _cacheLock = new object();

    /// <summary>
    /// Scans for types implementing the specified interface using assembly search patterns.
    /// </summary>
    /// <typeparam name="T">The interface type to search for implementations of</typeparam>
    /// <param name="searchPatterns">Assembly search patterns (e.g., "MyApp.*.dll")</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of types implementing the specified interface</returns>
    public async Task<IEnumerable<Type>> ScanForTypes<T>(
        IEnumerable<string> searchPatterns, 
        CancellationToken cancellationToken = default)
    {
        var interfaceType = typeof(T);
        var foundTypes = new List<Type>();

        // Discover assemblies using the search patterns
        var assemblies = await DiscoverAssemblies(searchPatterns, cancellationToken);

        // Search each assembly for implementing types
        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var types = GetImplementingTypes(assembly, interfaceType);
                foundTypes.AddRange(types);
            }
            catch (Exception)
            {
                // Log assembly scanning error but continue with other assemblies
                // Individual assembly failures shouldn't stop the entire scanning process
            }
        }

        return foundTypes.Distinct();
    }

    /// <summary>
    /// Discovers assemblies based on wildcard search patterns.
    /// </summary>
    /// <param name="searchPatterns">Assembly search patterns</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of discovered assemblies</returns>
    public async Task<IEnumerable<Assembly>> DiscoverAssemblies(
        IEnumerable<string> searchPatterns, 
        CancellationToken cancellationToken = default)
    {
        var allAssemblies = new List<Assembly>();

        foreach (var pattern in searchPatterns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var assemblies = await GetAssembliesForPattern(pattern, cancellationToken);
            allAssemblies.AddRange(assemblies);
        }

        return allAssemblies.Distinct();
    }

    /// <summary>
    /// Gets assemblies matching a specific wildcard pattern.
    /// Results are cached to improve performance on subsequent calls.
    /// </summary>
    private async Task<Assembly[]> GetAssembliesForPattern(string pattern, CancellationToken cancellationToken)
    {
        // Check cache first
        lock (_cacheLock)
        {
            if (_assemblyCache.TryGetValue(pattern, out var cachedAssemblies))
            {
                return cachedAssemblies;
            }
        }

        // Discover assemblies for this pattern
        var assemblies = await Task.Run(() => DiscoverAssembliesForPattern(pattern), cancellationToken);

        // Cache the results
        lock (_cacheLock)
        {
            _assemblyCache[pattern] = assemblies;
        }

        return assemblies;
    }

    /// <summary>
    /// Discovers assemblies for a single wildcard pattern.
    /// </summary>
    private Assembly[] DiscoverAssembliesForPattern(string pattern)
    {
        var assemblies = new List<Assembly>();

        try
        {
            var appDomain = AppDomain.CurrentDomain;
            var baseDirectory = appDomain.BaseDirectory;

            // Use Directory.GetFiles with wildcard pattern matching
            var assemblyFiles = Directory.GetFiles(baseDirectory, pattern, SearchOption.TopDirectoryOnly);

            foreach (var file in assemblyFiles)
            {
                try
                {
                    var assembly = LoadAssemblyFromFile(file);
                    if (assembly != null)
                    {
                        assemblies.Add(assembly);
                    }
                }
                catch (Exception)
                {
                    // Individual file loading failures should not stop the entire process
                    // This might happen for non-.NET assemblies or corrupted files
                }
            }
        }
        catch (Exception)
        {
            // Directory access or pattern matching failures
            // Return empty collection rather than throwing
        }

        return assemblies.ToArray();
    }

    /// <summary>
    /// Safely loads an assembly from a file path.
    /// </summary>
    private static Assembly? LoadAssemblyFromFile(string filePath)
    {
        try
        {
            // Check if file is likely a .NET assembly
            if (!IsLikelyDotNetAssembly(filePath))
            {
                return null;
            }

            // Try to load the assembly
            return Assembly.LoadFrom(filePath);
        }
        catch (BadImageFormatException)
        {
            // Not a valid .NET assembly
            return null;
        }
        catch (FileLoadException)
        {
            // Assembly could not be loaded
            return null;
        }
        catch (Exception)
        {
            // Other loading errors
            return null;
        }
    }

    /// <summary>
    /// Performs a basic check to determine if a file is likely a .NET assembly.
    /// </summary>
    private static bool IsLikelyDotNetAssembly(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".dll" || extension == ".exe";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets types from an assembly that implement the specified interface.
    /// </summary>
    private static IEnumerable<Type> GetImplementingTypes(Assembly assembly, Type interfaceType)
    {
        try
        {
            return assembly.GetTypes()
                .Where(type => IsValidImplementation(type, interfaceType));
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types in the assembly couldn't be loaded
            // Return the types that could be loaded
            return ex.Types
                .Where(type => type != null && IsValidImplementation(type!, interfaceType))!;
        }
        catch (Exception)
        {
            // Assembly type enumeration failed
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Determines if a type is a valid implementation of the specified interface.
    /// </summary>
    private static bool IsValidImplementation(Type type, Type interfaceType)
    {
        try
        {
            return interfaceType.IsAssignableFrom(type) &&
                   !type.IsInterface &&
                   !type.IsAbstract &&
                   HasPublicParameterlessConstructor(type);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a type has a public parameterless constructor.
    /// </summary>
    private static bool HasPublicParameterlessConstructor(Type type)
    {
        try
        {
            var constructor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            return constructor != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clears the assembly cache. Useful for testing or when assemblies may have changed.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _assemblyCache.Clear();
        }
    }

    /// <summary>
    /// Gets the number of cached assembly patterns.
    /// </summary>
    public int CachedPatternCount
    {
        get
        {
            lock (_cacheLock)
            {
                return _assemblyCache.Count;
            }
        }
    }
}
