namespace FluentCMS.Settings.Abstractions;

public interface ISettingsService
{
    Task<T?> Get<T>(string key, CancellationToken ct = default) where T : class, new();
    Task<string?> GetRawJson(string key, CancellationToken ct = default);
    Task Set<T>(string key, T value, CancellationToken ct = default) where T : class;
    Task SetRawJson(string key, string rawJson, string? valueType = null, CancellationToken ct = default);
    Task Invalidate(string key, CancellationToken ct = default);
}