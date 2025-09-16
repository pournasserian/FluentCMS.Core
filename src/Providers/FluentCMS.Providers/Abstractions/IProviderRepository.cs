namespace FluentCMS.Providers.Abstractions;

public interface IProviderRepository
{
    Task Add(string area, string name, string moduleTypeName, string options, bool isActive, string displayName, CancellationToken cancellationToken = default);
    Task Activate(string area, string name, CancellationToken cancellationToken = default);
    Task Deactivate(string area, string name, CancellationToken cancellationToken = default);
    Task UpdateOptions(string area, string name, string options, CancellationToken cancellationToken = default);
    Task Remove(string area, string name, CancellationToken cancellationToken = default);
}
