using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.Email;

/// <summary>
/// Interface for email providers.
/// </summary>
public interface IEmailProvider : IProvider
{
    /// <summary>
    /// Sends an email.
    /// </summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body.</param>
    /// <param name="isHtml">Whether the body is HTML.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends an email with attachments.
    /// </summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body.</param>
    /// <param name="attachments">The email attachments.</param>
    /// <param name="isHtml">Whether the body is HTML.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendEmailWithAttachmentsAsync(
        string to, 
        string subject, 
        string body, 
        EmailAttachment[] attachments, 
        bool isHtml = false, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an email attachment.
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// Gets or sets the attachment file name.
    /// </summary>
    public string FileName { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the attachment content.
    /// </summary>
    public byte[] Content { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the attachment content type.
    /// </summary>
    public string ContentType { get; set; } = null!;
}
