namespace FluentCMS.Providers.Email.Abstractions;

public interface IEmailProvider
{
    Task Send(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
