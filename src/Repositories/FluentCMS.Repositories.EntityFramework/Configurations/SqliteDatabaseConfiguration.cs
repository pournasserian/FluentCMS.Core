namespace FluentCMS.Repositories.EntityFramework.Configurations;

public class SqliteDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
{
    public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(connectionString);
    }
}
