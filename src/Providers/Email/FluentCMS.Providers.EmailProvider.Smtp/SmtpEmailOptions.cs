namespace FluentCMS.Providers.EmailProvider.Smtp;

/// <summary>
/// Configuration options for the SMTP email provider.
/// </summary>
public class SmtpEmailOptions
{
    /// <summary>
    /// Gets or sets the SMTP server host.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the SMTP server port.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Gets or sets a value indicating whether to use SSL.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use default credentials.
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = false;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    public string FromEmail { get; set; } = "noreply@example.com";

    /// <summary>
    /// Gets or sets the sender's display name.
    /// </summary>
    public string FromName { get; set; } = "System Notification";
}
