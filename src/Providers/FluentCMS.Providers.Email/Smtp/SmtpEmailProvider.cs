using System.Net;
using System.Net.Mail;
using FluentCMS.Providers.Abstractions;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Providers.Email.Smtp;

/// <summary>
/// SMTP implementation of the email provider.
/// </summary>
public class SmtpEmailProvider : ProviderBase, IEmailProvider, IProviderWithOptions<SmtpEmailOptions>, IProviderLifecycle, IProviderHealth
{
    private SmtpEmailOptions _options = new();
    private readonly ILogger<SmtpEmailProvider>? _logger;
    private bool _isInitialized;
    private bool _isActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpEmailProvider"/> class.
    /// </summary>
    public SmtpEmailProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpEmailProvider"/> class with a logger.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SmtpEmailProvider(ILogger<SmtpEmailProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Configure(SmtpEmailOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger?.LogInformation("SMTP Email Provider configured with server: {Host}:{Port}", options.Host, options.Port);
    }

    /// <inheritdoc />
    public override Task Initialize(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return Task.CompletedTask;
        }

        _logger?.LogInformation("Initializing SMTP Email Provider");
        
        // Validation and setup
        if (string.IsNullOrEmpty(_options.Host))
        {
            throw new ProviderConfigurationException("SMTP Host cannot be empty");
        }

        if (_options.Port <= 0)
        {
            throw new ProviderConfigurationException("SMTP Port must be a positive number");
        }

        if (string.IsNullOrEmpty(_options.FromEmail))
        {
            throw new ProviderConfigurationException("From Email cannot be empty");
        }

        try
        {
            // Verify the FromEmail is valid
            _ = new MailAddress(_options.FromEmail, _options.FromName);
        }
        catch (FormatException ex)
        {
            throw new ProviderConfigurationException("Invalid From Email address format", ex);
        }

        _isInitialized = true;
        _logger?.LogInformation("SMTP Email Provider initialized");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task Activate(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new ProviderException("Provider must be initialized before activation");
        }

        _isActive = true;
        _logger?.LogInformation("SMTP Email Provider activated");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task Deactivate(CancellationToken cancellationToken = default)
    {
        _isActive = false;
        _logger?.LogInformation("SMTP Email Provider deactivated");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task Uninstall(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("SMTP Email Provider uninstalled");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<(ProviderHealthStatus Status, string Message)> GetStatus(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            return Task.FromResult((ProviderHealthStatus.Warning, "Provider is not initialized"));
        }

        if (!_isActive)
        {
            return Task.FromResult((ProviderHealthStatus.Inactive, "Provider is not active"));
        }

        // Basic connectivity check to the SMTP server (doesn't actually send an email)
        try
        {
            using var client = CreateSmtpClient();
            return Task.FromResult((ProviderHealthStatus.Healthy, "SMTP server is reachable"));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SMTP server health check failed");
            return Task.FromResult((ProviderHealthStatus.Unhealthy, $"SMTP server is not reachable: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        EnsureActive();

        var message = CreateMailMessage(to, subject, body, isHtml);
        await SendMailMessageAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendEmailWithAttachmentsAsync(
        string to, 
        string subject, 
        string body, 
        EmailAttachment[] attachments, 
        bool isHtml = false, 
        CancellationToken cancellationToken = default)
    {
        EnsureActive();

        var message = CreateMailMessage(to, subject, body, isHtml);
        
        // Add attachments
        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                var ms = new System.IO.MemoryStream(attachment.Content);
                message.Attachments.Add(new Attachment(ms, attachment.FileName, attachment.ContentType));
            }
        }

        await SendMailMessageAsync(message, cancellationToken);
    }

    private MailMessage CreateMailMessage(string to, string subject, string body, bool isHtml)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        
        message.To.Add(to);
        
        return message;
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = _options.UseDefaultCredentials
        };

        if (!_options.UseDefaultCredentials && !string.IsNullOrEmpty(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        return client;
    }

    private async Task SendMailMessageAsync(MailMessage message, CancellationToken cancellationToken)
    {
        using var client = CreateSmtpClient();

        // Implement retry logic
        int attempts = 0;
        Exception? lastException = null;

        while (attempts < _options.MaxSendAttempts)
        {
            try
            {
                attempts++;
                
                // Use cancelable send
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout
                
                await Task.Factory.FromAsync(
                    client.BeginSend(message, null, null),
                    client.EndSend);
                
                _logger?.LogInformation("Email sent successfully to {Recipient} with subject: {Subject}", 
                    string.Join(", ", message.To), message.Subject);
                
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogWarning("Email sending canceled");
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Email sending attempt {Attempt}/{MaxAttempts} failed", 
                    attempts, _options.MaxSendAttempts);
                
                if (attempts < _options.MaxSendAttempts)
                {
                    await Task.Delay(_options.SendRetryDelayMs, cancellationToken);
                }
            }
        }

        throw new ProviderException($"Failed to send email after {_options.MaxSendAttempts} attempts", lastException);
    }

    private void EnsureActive()
    {
        if (!_isInitialized)
        {
            throw new ProviderException("SMTP Email Provider is not initialized");
        }

        if (!_isActive)
        {
            throw new ProviderException("SMTP Email Provider is not active");
        }
    }
}
