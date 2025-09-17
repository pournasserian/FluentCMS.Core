using FluentCMS.Providers.EmailProviders.Abstractions;

namespace FluentCMS.Providers.EmailProviders;

public class NullEmailProvider : IEmailProvider
{
    public Task Send(string recipient, string subject, string body, IDictionary<string, string>? headers = null)
    {
        return Task.CompletedTask;
    }
}