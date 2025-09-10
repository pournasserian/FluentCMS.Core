using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FluentCMS.Configuration;

public interface IOptionsRepository
{
    Task EnsureCreated(CancellationToken cancellationToken = default);
    Task Upsert(OptionRegistration registration, object options, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string?>> GetAllSections(CancellationToken cancellationToken = default);

}

internal class SqliteOptionsRepository(string connectionString) : IOptionsRepository
{
    private const string CreateSql = @"CREATE TABLE IF NOT EXISTS Options (
            Section TEXT PRIMARY KEY,
            Value    TEXT NOT NULL
        );";

    private const string SelectAllSql = "SELECT * FROM Options;";
    
    private const string UpsertSql = @"INSERT INTO Options (Section, Value)
            VALUES ($section, $value)
            ON CONFLICT(Section) DO NOTHING;";


    public async Task EnsureCreated(CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = CreateSql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string?>> GetAllSections(CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectAllSql;
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
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
        return data;
    }

    public Task Upsert(OptionRegistration registration, object options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

}
