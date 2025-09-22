namespace FluentCMS.Plugins.AuditTrailManager;

public class AuditTrailDataSeeder(IDatabaseManager<IAuditTrailDatabaseMarker> databaseManager) : IDataSeeder
{
    public int Priority => 10;

    public async Task<bool> HasData(CancellationToken cancellationToken = default)
    {
        return !await databaseManager.TablesEmpty(["AuditTrails"], cancellationToken);
    }

    public Task SeedData(CancellationToken cancellationToken = default)
    {
        // do nothing
        return Task.CompletedTask;
    }
}
