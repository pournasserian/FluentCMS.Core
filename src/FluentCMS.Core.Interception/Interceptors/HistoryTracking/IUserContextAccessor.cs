namespace FluentCMS.Core.Interception.Interceptors.HistoryTracking;

/// <summary>
/// Provides access to information about the current user context.
/// </summary>
public interface IUserContextAccessor
{
    /// <summary>
    /// Gets the username of the current user.
    /// </summary>
    /// <returns>The username of the current user, or a default value if no user is authenticated.</returns>
    string GetCurrentUsername();
}
