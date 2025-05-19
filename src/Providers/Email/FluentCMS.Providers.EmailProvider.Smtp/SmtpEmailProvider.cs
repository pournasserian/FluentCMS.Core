namespace FluentCMS.Providers.EmailProvider.Smtp;

[ProviderMetadata<IEmailProvider>(
    category: "Email", 
    name: "Smtp Server",
    description: "Smtp email provider",
    version: "1.0.0", 
    isDefault: true)]
public class SmtpEmailProvider(ILogger<SmtpEmailProvider> logger, IOptionsMonitor<SmtpEmailOptions> options) : IEmailProvider
{
    private readonly SmtpEmailOptions emailOptions = options.CurrentValue;

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(emailOptions.FromEmail, emailOptions.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(to);
            using var client = new SmtpClient(emailOptions.Host, emailOptions.Port)
            {
                EnableSsl = emailOptions.EnableSsl,
                UseDefaultCredentials = emailOptions.UseDefaultCredentials
            };

            if (!emailOptions.UseDefaultCredentials && !string.IsNullOrEmpty(emailOptions.Username))
            {
                client.Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password);
            }
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log the error and rethrow or handle it as needed
            logger.LogError("Failed to send email to {To}. Subject: {Subject}. Error: {Error}", to, subject, ex.Message);
        }
    }
}