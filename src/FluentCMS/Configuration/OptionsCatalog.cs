using Microsoft.Extensions.Options;

namespace FluentCMS.Configuration;

public interface IOptionsCatalog
{
    IReadOnlyCollection<OptionRegistration> All { get; }
    //void Add(OptionRegistration optionRegistration);
}

internal sealed class OptionsCatalog(IOptions<ProviderCatalogOptions> options) : IOptionsCatalog
{
    public IReadOnlyCollection<OptionRegistration> All => [.. options.Value.Types];

    //public void Add(OptionRegistration optionRegistration)
    //{
    //    options.Value.Types.Add(optionRegistration);
    //}
}

public sealed class ProviderCatalogOptions
{
    // Use HashSet to avoid duplicates 
    public HashSet<OptionRegistration> Types { get; } = [];
}