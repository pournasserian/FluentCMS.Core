namespace FluentCMS.Repositories.EntityFramework.Configurations;

public class SqlServerDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
{
    public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }
}
