namespace FluentCMS.Database.Abstractions;

public interface IDatabaseManager
{
    /// <summary>
    /// Checks if the database exists.
    /// </summary>
    Task<bool> DatabaseExists(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the database if it does not already exist.
    /// </summary>
    Task CreateDatabase(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified tables exist in the database.
    /// </summary>
    Task<bool> TablesExist(IEnumerable<string> tableNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified tables are empty.
    /// </summary>
    Task<bool> TablesEmpty(IEnumerable<string> tableNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL command against the database.
    /// </summary>
    Task ExecuteRaw(string sql, CancellationToken cancellationToken = default);
}