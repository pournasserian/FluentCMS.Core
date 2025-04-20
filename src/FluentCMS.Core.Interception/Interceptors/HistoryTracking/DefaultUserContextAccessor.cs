namespace FluentCMS.Core.Interception.Interceptors.HistoryTracking;

/// <summary>
/// Default implementation of IUserContextAccessor that returns a fixed username or "System" by default.
/// This can be used for testing or simple scenarios where a more sophisticated user context is not needed.
/// </summary>
public class DefaultUserContextAccessor : IUserContextAccessor
{
    private readonly string _defaultUsername;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserContextAccessor"/> class.
    /// </summary>
    /// <param name="defaultUsername">The default username to return. If null or empty, "System" will be used.</param>
    public DefaultUserContextAccessor(string? defaultUsername = null)
    {
        _defaultUsername = string.IsNullOrEmpty(defaultUsername) ? "System" : defaultUsername;
    }

    /// <inheritdoc />
    public string GetCurrentUsername()
    {
        return _defaultUsername;
    }
}
