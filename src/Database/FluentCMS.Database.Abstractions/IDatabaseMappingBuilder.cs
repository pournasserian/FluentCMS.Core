using System.Reflection;

namespace FluentCMS.Database.Abstractions;

/// <summary>
/// Fluent builder for configuring database mappings for different assemblies.
/// </summary>
public interface IDatabaseMappingBuilder
{
    /// <summary>
    /// Configures the default database provider for assemblies that don't have explicit mappings.
    /// </summary>
    /// <returns>An assembly mapping builder to configure the default database provider.</returns>
    IAssemblyMappingBuilder SetDefault();

    /// <summary>
    /// Maps a specific assembly to use a particular database provider.
    /// </summary>
    /// <typeparam name="T">A type from the assembly to map.</typeparam>
    /// <returns>An assembly mapping builder to configure the database provider for this assembly.</returns>
    IAssemblyMappingBuilder MapAssembly<T>();

    /// <summary>
    /// Maps a specific assembly to use a particular database provider.
    /// </summary>
    /// <param name="assembly">The assembly to map.</param>
    /// <returns>An assembly mapping builder to configure the database provider for this assembly.</returns>
    IAssemblyMappingBuilder MapAssembly(Assembly assembly);
}
