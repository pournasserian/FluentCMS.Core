using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.EmailProvider.Abstractions;

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
}