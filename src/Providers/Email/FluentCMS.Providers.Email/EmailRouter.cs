using FluentCMS.Providers.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentCMS.Providers.Email;

//public sealed class EmailRouter(IOptionsMonitor<EmailOptions> options, IServiceProvider sp) : IEmailProvider
//{
//    public Task Send(string to, string subject, string htmlBody, CancellationToken ct = default)
//    {
//        var key = options.CurrentValue.ProviderName?.ToLowerInvariant();
//        var impl = sp.GetRequiredKeyedService<IEmailProvider>(key);
//        return impl.Send(to, subject, htmlBody, ct);
//    }
//}
