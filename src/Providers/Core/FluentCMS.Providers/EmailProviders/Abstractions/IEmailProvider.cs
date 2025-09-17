using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.EmailProviders.Abstractions;

public interface IEmailProvider : IProvider
{
    Task Send(string recipient, string subject, string body, IDictionary<string, string>? headers = null);
}