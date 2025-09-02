using FluentCMS.Settings.Abstractions;
using System.Text.Json;

namespace FluentCMS.Settings.Json;

public sealed class JsonFileSettingsRepository : ISettingsRepository, IDisposable
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly FileSystemWatcher _watcher;
    private volatile bool _self;
    private Dictionary<string, SettingRecord> _cache = new(StringComparer.OrdinalIgnoreCase);

    public event Action? ExternalChanged;

    public JsonFileSettingsRepository(string path)
    {
        _path = path;

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        if (!File.Exists(_path))
            File.WriteAllText(_path, "{}");

        _watcher = new(Path.GetDirectoryName(_path)!, Path.GetFileName(_path))
        { EnableRaisingEvents = true, NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName };
        _watcher.Changed += (_, __) => { if (!_self) ExternalChanged?.Invoke(); };
        _watcher.Created += (_, __) => { if (!_self) ExternalChanged?.Invoke(); };
        _watcher.Renamed += (_, __) => { if (!_self) ExternalChanged?.Invoke(); };
    }

    public async Task<SettingRecord?> Get(string key, CancellationToken ct = default)
        => (await GetAll(ct)).TryGetValue(key, out var rec) ? rec : null;

    public async Task<IDictionary<string, SettingRecord>> GetAll(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await using var fs = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
            var dict = new Dictionary<string, SettingRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in doc.RootElement.EnumerateObject())
            {
                var o = kv.Value;
                dict[kv.Name] = new SettingRecord(
                    o.GetProperty("valueType").GetString()!,
                    o.GetProperty("valueJson").GetRawText());
            }
            _cache = dict;
            return dict;
        }
        finally { _gate.Release(); }
    }

    public async Task Upsert(string key, SettingRecord rec, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var data = _cache.Count == 0 ? (await GetAll(ct)).ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase) : new(_cache, StringComparer.OrdinalIgnoreCase);
            data[key] = rec;

            var tmp = _path + ".tmp";
            _self = true;
            await using (var fs = File.Open(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            await using (var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
            {
                w.WriteStartObject();
                foreach (var (k, r) in data)
                {
                    w.WritePropertyName(k);
                    w.WriteStartObject();
                    w.WriteString("valueType", r.ValueType);
                    w.WritePropertyName("valueJson");
                    using var el = JsonDocument.Parse(r.ValueJson);
                    el.RootElement.WriteTo(w);
                    w.WriteEndObject();
                }
                w.WriteEndObject();
            }

            File.Move(tmp, _path, true);

            _cache = data;
        }
        finally
        {
            _self = false;
            _gate.Release();
        }
    }

    public void Dispose() => _watcher.Dispose();
}
