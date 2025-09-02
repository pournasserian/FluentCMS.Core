using FluentCMS.Providers.Email.Abstractions;

namespace FluentCMS.Providers.Email;

public class NopEmailProvider : IEmailProvider
{
    public Task Send(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
