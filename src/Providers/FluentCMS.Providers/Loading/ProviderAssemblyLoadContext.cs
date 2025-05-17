namespace FluentCMS.Providers.Loading;

/// <summary>
/// Custom assembly load context for loading provider assemblies.
/// This context can be unloaded when the provider is no longer needed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProviderAssemblyLoadContext"/> class.
/// </remarks>
/// <param name="assemblyPath">The path to the provider assembly.</param>
internal class ProviderAssemblyLoadContext(string assemblyPath) : AssemblyLoadContext(Path.GetFileNameWithoutExtension(assemblyPath), isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(assemblyPath);

    /// <summary>
    /// Loads the provider assembly.
    /// </summary>
    /// <returns>The loaded assembly.</returns>
    public Assembly LoadProviderAssembly()
    {
        return LoadFromAssemblyPath(assemblyPath);
    }

    /// <summary>
    /// Resolves an assembly name to an assembly.
    /// </summary>
    /// <param name="assemblyName">The assembly name to resolve.</param>
    /// <returns>The resolved assembly or null if not found.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve the assembly using the dependency resolver
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // If not found, return null to let the default load context handle it
        return null;
    }

    /// <summary>
    /// Resolves an unmanaged DLL to a path.
    /// </summary>
    /// <param name="unmanagedDllName">The name of the unmanaged DLL.</param>
    /// <returns>The path to the unmanaged DLL or null if not found.</returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // Try to resolve the unmanaged DLL using the dependency resolver
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        // If not found, return IntPtr.Zero to let the default load context handle it
        return IntPtr.Zero;
    }
}
