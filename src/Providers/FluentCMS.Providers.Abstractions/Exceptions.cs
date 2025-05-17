namespace FluentCMS.Providers.Abstractions;

/// <summary>
/// Base exception for provider system errors.
/// </summary>
[Serializable]
public class ProviderException : Exception
{
    public ProviderException() { }

    public ProviderException(string message) : base(message) { }

    public ProviderException(string message, Exception innerException) : base(message, innerException) { }

    protected ProviderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Thrown when a provider cannot be found.
/// </summary>
[Serializable]
public class ProviderNotFoundException : ProviderException
{
    public ProviderNotFoundException() : base("The specified provider could not be found.") { }

    public ProviderNotFoundException(string message) : base(message) { }

    public ProviderNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    protected ProviderNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Thrown when a provider fails to activate.
/// </summary>
[Serializable]
public class ProviderActivationException : ProviderException
{
    public ProviderActivationException() : base("The provider could not be activated.") { }

    public ProviderActivationException(string message) : base(message) { }

    public ProviderActivationException(string message, Exception innerException) : base(message, innerException) { }

    protected ProviderActivationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Thrown when a provider fails to load.
/// </summary>
[Serializable]
public class ProviderLoadException : ProviderException
{
    public ProviderLoadException() : base("The provider assembly could not be loaded.") { }

    public ProviderLoadException(string message) : base(message) { }

    public ProviderLoadException(string message, Exception innerException) : base(message, innerException) { }

    protected ProviderLoadException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Thrown when a provider configuration is invalid.
/// </summary>
[Serializable]
public class ProviderConfigurationException : ProviderException
{
    public string[] ConfigurationErrors { get; }

    public ProviderConfigurationException() : base("The provider configuration is invalid.") 
    {
        ConfigurationErrors = Array.Empty<string>();
    }

    public ProviderConfigurationException(string message) : base(message) 
    {
        ConfigurationErrors = Array.Empty<string>();
    }

    public ProviderConfigurationException(string message, string[] configurationErrors) : base(message)
    {
        ConfigurationErrors = configurationErrors ?? Array.Empty<string>();
    }

    public ProviderConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
        ConfigurationErrors = [];
    }

    protected ProviderConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ConfigurationErrors = (string[])info.GetValue(nameof(ConfigurationErrors), typeof(string[])) ?? [];
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ConfigurationErrors), ConfigurationErrors);
    }
}

/// <summary>
/// Thrown when a provider is already active.
/// </summary>
[Serializable]
public class ProviderAlreadyActiveException : ProviderException
{
    public ProviderAlreadyActiveException() : base("A provider of this type is already active.") { }

    public ProviderAlreadyActiveException(string message) : base(message) { }

    public ProviderAlreadyActiveException(string message, Exception innerException) : base(message, innerException) { }

    protected ProviderAlreadyActiveException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Thrown when a provider cannot be uninstalled.
/// </summary>
[Serializable]
public class ProviderUninstallException : ProviderException
{
    public ProviderUninstallException() : base("The provider could not be uninstalled.") { }

    public ProviderUninstallException(string message) : base(message) { }

    public ProviderUninstallException(string message, Exception innerException) : base(message, innerException) { }

    protected ProviderUninstallException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
