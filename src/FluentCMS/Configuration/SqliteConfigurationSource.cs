namespace FluentCMS.Configuration;

public sealed class SqliteConfigurationSource : IConfigurationSource
{
    public string ConnectionString { get; set; } = default!;
    public TimeSpan? ReloadInterval { get; set; }
    public SqliteConfigurationProvider Provider { get; private set; } = default!;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        Provider = new SqliteConfigurationProvider(this);
        return Provider;
    }
}