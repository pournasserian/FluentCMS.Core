namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Flag attribute to mark types as providers.
/// All provider implementations should be decorated with this attribute.
/// Constructor arguments are used to provide metadata about the provider.
/// Constructor arguments order is important and should be kept in sync with the provider scanner.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ProviderMetadataAttribute<TProviderInterface>(
    string category,
    string name,
    string? description = null,
    string version = "1.0.0",
    bool isDefault = false): 
    Attribute,
    IProviderMetadataAttributeAnchor
    where TProviderInterface : class, IProvider
{
    /// <summary>
    /// Gets the category of this provider implementation.
    /// </summary>
    public string Category { get; } = category;

    /// <summary>
    /// Gets a value indicating whether this provider is the default implementation for its category.
    /// </summary>
    public bool IsDefault { get; } = isDefault;

    /// <summary>
    /// Gets the display name of this provider implementation.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the description of this provider implementation.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    /// Gets the version of this provider implementation.
    /// </summary>
    public string Version { get; } = version;

    /// <summary>
    /// Gets the type of the interface that this provider implements.
    /// </summary>
    public Type InterfaceType { get; } = typeof(TProviderInterface);
}
// <summary>

public interface IProviderMetadataAttributeAnchor
{
}