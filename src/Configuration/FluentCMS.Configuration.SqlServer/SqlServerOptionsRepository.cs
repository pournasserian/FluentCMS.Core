using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FluentCMS.Configuration.SqlServer;

internal class SqlServerOptionsRepository(string connectionString) : IOptionsRepository
{
    // Use NVARCHAR to store text; 450 keeps PK within SQL Server's index key size limit (900 bytes).
    private const string CreateSql = @"
            IF OBJECT_ID(N'dbo.Options', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Options
                (
                    Section NVARCHAR(450) NOT NULL PRIMARY KEY,
                    Value   NVARCHAR(MAX) NOT NULL
                );
            END
            ";

    private const string SelectAllSql = "SELECT Section, Value FROM dbo.Options;";

    // Upsert implemented as: UPDATE first; if no rows affected, INSERT.
    private const string UpdateSql = @"
            UPDATE dbo.Options
            SET Value = @value
            WHERE Section = @section;
            ";

    private const string InsertSql = @"
            INSERT INTO dbo.Options (Section, Value)
            VALUES (@section, @value);
            ";

    public async Task EnsureCreated(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = CreateSql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string?>> GetAllSections(CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectAllSql;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var section = reader.GetString(0);
            var json = reader.GetString(1);

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

    public async Task<int> Upsert(OptionRegistration registration, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        // We’ll try UPDATE first; if it didn’t touch anything, do INSERT.
        await using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = UpdateSql;
        AddParameters(updateCmd, registration);

        var affected = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        if (affected > 0)
            return affected;

        await using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = InsertSql;
        AddParameters(insertCmd, registration);

        try
        {
            affected = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            return affected;
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // PK/Unique violation
        {
            // Race: someone inserted between our UPDATE and INSERT. Treat as success (0 or 1).
            return 0;
        }
    }

    private static void AddParameters(SqlCommand cmd, OptionRegistration reg)
    {
        // Explicit sizes/types to avoid AddWithValue pitfalls.
        var pSection = new SqlParameter("@section", SqlDbType.NVarChar, 450) { Value = reg.Section };
        var pValue = new SqlParameter("@value", SqlDbType.NVarChar, -1) { Value = reg.DefaultValue ?? (object)DBNull.Value };

        cmd.Parameters.Add(pSection);
        cmd.Parameters.Add(pValue);
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
