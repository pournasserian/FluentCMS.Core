using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FluentCMS.Configuration;

/// <summary>
/// Loads configuration from a SQLite table where each row = one section, stored as JSON.
/// Table schema: Options (Section TEXT PRIMARY KEY, Json TEXT NOT NULL, UpdatedAt TEXT)
/// </summary>
public sealed class SqliteConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly SqliteConfigurationSource _source;
    private readonly Timer? _timer;
    private const string CreateSql = @"CREATE TABLE IF NOT EXISTS Options (
            Section TEXT PRIMARY KEY,
            Value    TEXT NOT NULL
        );";

    public SqliteConfigurationProvider(SqliteConfigurationSource source)
    {
        _source = source;
        EnsureCreated();
        if (_source.ReloadInterval is { } interval)
        {
            _timer = new Timer(_ => TriggerReload(), null, interval, interval);
        }
    }

    private void EnsureCreated()
    {
        using var conn = new SqliteConnection(_source.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = CreateSql;
        cmd.ExecuteNonQuery();
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        using var conn = new SqliteConnection(_source.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Options";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var section = reader.GetString(0);
            var json = reader.GetString(1);

            // Flatten JSON into config keys: Section:Child:SubChild = value
            try
            {
                var node = JsonNode.Parse(json);
                if (node is JsonObject obj)
                {
                    FlattenJson(data, section, obj);
                }
            }
            catch
            {
                // Ignore malformed rows
            }
        }
        Data = data;
    }

    private static void FlattenJson(Dictionary<string, string?> data, string prefix, JsonObject obj)
    {
        foreach (var kvp in obj)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";
            switch (kvp.Value)
            {
                case JsonObject childObj:
                    FlattenJson(data, key, childObj);
                    break;
                case JsonArray arr:
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var item = arr[i];
                        if (item is JsonObject itemObj)
                        {
                            FlattenJson(data, $"{key}:{i}", itemObj);
                        }
                        else
                        {
                            data[$"{key}:{i}"] = item?.ToJsonString();
                        }
                    }
                    break;
                default:
                    data[key] = kvp.Value?.GetValueKind() is JsonValueKind.String
                        ? kvp.Value!.ToString()
                        : kvp.Value?.ToJsonString();
                    break;
            }
        }
    }

    /// <summary>
    /// Call after you change DB programmatically to force consumers to refresh.
    /// </summary>
    public void TriggerReload()
    {
        Load();
        OnReload();
    }

    public void Dispose() => _timer?.Dispose();
}
