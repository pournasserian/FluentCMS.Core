namespace FluentCMS.Core;

/// <summary>
/// ApiExecutionContext encapsulates various contextual information 
/// about the current API request, such as trace ID, user identity, 
/// session ID, and more. This class is useful for logging, auditing, 
/// and controlling request-specific behavior.
/// </summary>
public class ApiExecutionContext
{
    public string TraceId { get; set; } = string.Empty;         // Unique identifier for the current request
    public string UniqueId { get; set; } = string.Empty;        // Unique identifier for the user, often set by the client
    public string SessionId { get; set; } = string.Empty;       // Unique session identifier
    public string UserIp { get; set; } = string.Empty;          // IP address of the user making the request
    public string Language { get; set; } = string.Empty;        // Preferred language of the user, defaults to 'en-US'
    public DateTime StartDate { get; set; } = DateTime.UtcNow;  // Timestamp when the request was initiated
    public Guid UserId { get; set; } = Guid.Empty;              // User ID extracted from the user's claims, default is Guid.Empty
    public string Username { get; set; } = string.Empty;        // Username extracted from the user's claims, default is empty string
    public bool IsAuthenticated { get; set; } = false;          // Indicates if the user is authenticated, default is false
}
