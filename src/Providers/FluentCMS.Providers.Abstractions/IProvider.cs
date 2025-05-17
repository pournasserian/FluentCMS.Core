namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Flag interface to mark types as providers.
/// All provider interfaces should inherit from this interface.
/// </summary>
public interface IProvider
{
    /// <summary>
    /// Gets the unique identifier for this provider implementation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of this provider implementation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this provider implementation.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the version of this provider implementation.
    /// </summary>
    string Version { get; }
}
