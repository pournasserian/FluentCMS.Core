using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.Email.Null;

public class NullEmailProviderModule : ProviderModuleBase<NullEmailProvider>
{
    public override string Area => "Email";

    public override string DisplayName => "Null Email Provider (No-Op)";
}
