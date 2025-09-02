using FluentCMS.Settings.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace FluentCMS.Settings;

public sealed class SettingsService(ISettingsRepository settingsRepository, IMemoryCache cache, ISettingsChangeNotifier notifier) : ISettingsService
{
    private static string CK(string key) => $"appsettings::{key}";

    public async Task<T?> Get<T>(string key, CancellationToken ct = default) where T : class, new()
    {
        var raw = await GetRawJson(key, ct);
        return raw is null ? null : JsonSerializer.Deserialize<T>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<string?> GetRawJson(string key, CancellationToken ct = default)
    {
        if (cache.TryGetValue(CK(key), out string? cached)) return cached;
        var rec = await settingsRepository.Get(key, ct);
        if (rec is null) return null;
        cache.Set(CK(key), rec.ValueJson);
        return rec.ValueJson;
    }

    public Task Set<T>(string key, T value, CancellationToken ct = default) where T : class
        => SetRawJson(key, JsonSerializer.Serialize(value), value.GetType().AssemblyQualifiedName, ct);

    public async Task SetRawJson(string key, string rawJson, string? valueType = null, CancellationToken ct = default)
    {
        await settingsRepository.Upsert(key, new SettingRecord(valueType ?? typeof(object).AssemblyQualifiedName!, rawJson), ct);
        cache.Set(CK(key), rawJson);
        notifier.SignalChanged(key);
    }

    public Task Invalidate(string key, CancellationToken ct = default)
    {
        cache.Remove(CK(key));
        notifier.SignalChanged(key);
        return Task.CompletedTask;
    }
}
