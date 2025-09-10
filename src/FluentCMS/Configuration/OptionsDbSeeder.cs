using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace FluentCMS.Configuration;

/// <summary>
/// On startup: ensure table exists and seed any registered option sections
/// from the current IConfiguration (typically appsettings) if missing.
/// </summary>
public sealed class OptionsDbSeeder(ILogger<OptionsDbSeeder> logger,
                                    IConfiguration config,
                                    IOptionsCatalog registrations,
                                    SqliteConfigurationSource source)
    : IHostedService
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private const string UpsertSql = @"INSERT INTO Options (Section, Value)
            VALUES ($section, $value)
            ON CONFLICT(Section) DO NOTHING;";

    private const string EnsureSql = @"CREATE TABLE IF NOT EXISTS Options (
            Section TEXT PRIMARY KEY,
            Value   TEXT NOT NULL);";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var conn = new SqliteConnection(source.ConnectionString);
            conn.Open();
            // Ensure table exists
            using (var ensure = conn.CreateCommand())
            {
                ensure.CommandText = EnsureSql;
                ensure.ExecuteNonQuery();
            }

            foreach (var reg in registrations.All)
            {
                var section = config.GetSection(reg.Section);
                if (!section.Exists())
                {
                    logger.LogInformation("No appsettings section found for {Section}; skipping seed.", reg.Section);
                    continue;
                }

                // Serialize the bound object so data annotations & defaults apply
                var bound = section.Get(reg.Type) ?? Activator.CreateInstance(reg.Type)!;
                var json = JsonSerializer.Serialize(bound, reg.Type, jsonSerializerOptions);

                using var cmd = conn.CreateCommand();
                cmd.CommandText = UpsertSql;
                cmd.Parameters.AddWithValue("$section", reg.Section);
                cmd.Parameters.AddWithValue("$value", json);
                var rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    logger.LogInformation("Seeded options for {Section}", reg.Section);
                }
            }

            // After seeding, refresh the configuration provider so IOptions binds from DB
            source.Provider.TriggerReload();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed seeding options DB");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
