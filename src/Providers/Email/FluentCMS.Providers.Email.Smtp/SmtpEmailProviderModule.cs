using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.Email.Smtp;

public class SmtpEmailProviderModule : ProviderModuleBase<SmtpEmailProvider, SmtpEmailProviderOptions>
{
    public override string Area => "Email";

    public override string DisplayName => "SMTP Email Provider";
}
