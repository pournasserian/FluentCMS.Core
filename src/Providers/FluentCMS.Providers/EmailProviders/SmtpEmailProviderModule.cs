using FluentCMS.Providers.Core;

namespace FluentCMS.Providers.EmailProviders;

public class SmtpEmailProviderModule : ProviderModuleBase<SmtpEmailProvider, SmtpEmailProviderOptions>
{
    public override string Area => "Email";

    public override string DisplayName => "SMTP Email Provider";
}
