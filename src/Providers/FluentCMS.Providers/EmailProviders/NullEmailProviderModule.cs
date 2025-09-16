using FluentCMS.Providers.Core;

namespace FluentCMS.Providers.EmailProviders;

public class NullEmailProviderModule : ProviderModuleBase<NullEmailProvider, NullEmailProviderOptions>
{
    public override string Area => "Email";

    public override string DisplayName => "Null Email Provider (No-Op)";
}
