namespace FluentCMS.Providers;

/// <summary>
/// Factory for resolving provider instances
/// </summary>
/// <typeparam name="T">The provider interface type</typeparam>
public interface IProviderFactory<T> where T : class
{
    /// <summary>
    /// Gets the active provider instance for the specified provider type
    /// </summary>
    /// <returns>The active provider instance</returns>
    T GetActiveProvider();

    /// <summary>
    /// Gets a named provider instance
    /// </summary>
    /// <param name="name">The provider instance name</param>
    /// <returns>The named provider instance</returns>
    T GetProvider(string name);

    /// <summary>
    /// Gets all registered provider instances of the specified type
    /// </summary>
    /// <returns>Dictionary of provider name to provider instance</returns>
    IReadOnlyDictionary<string, T> GetAllProviders();

    /// <summary>
    /// Checks if a provider with the specified name exists
    /// </summary>
    /// <param name="name">The provider instance name</param>
    /// <returns>True if the provider exists, false otherwise</returns>
    bool HasProvider(string name);
}
