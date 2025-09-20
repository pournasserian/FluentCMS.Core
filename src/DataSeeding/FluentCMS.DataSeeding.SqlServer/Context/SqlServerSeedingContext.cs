using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataSeeding.SqlServer.Context;

/// <summary>
/// SQL Server-specific implementation of SeedingContext.
/// Provides database connection management and service access for SQL Server databases.
/// </summary>
public class SqlServerSeedingContext : SeedingContext
{
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;
    private readonly SqlServerDataSeedingOptions _options;
    private SqlConnection? _connection;

    /// <summary>
    /// Initializes a new instance of SqlServerSeedingContext with connection string and service provider.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    /// <param name="options">SQL Server data seeding options</param>
    public SqlServerSeedingContext(
        string connectionString, 
        IServiceProvider serviceProvider, 
        SqlServerDataSeedingOptions options)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets a SQL Server database connection.
    /// Creates a new connection on each call to ensure thread safety and proper resource management.
    /// </summary>
    /// <returns>A SQL Server database connection</returns>
    public override IDbConnection GetConnection()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(_connectionString);
        
        // Apply MARS setting if configured
        if (_options.EnableMars)
        {
            connectionStringBuilder.MultipleActiveResultSets = true;
        }

        return new SqlConnection(connectionStringBuilder.ToString());
    }

    /// <summary>
    /// Gets a required service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The requested service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not found</exception>
    public override T GetRequiredService<T>()
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the dependency injection container, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The requested service instance or null if not found</returns>
    public override T? GetService<T>() where T : class
    {
        return _serviceProvider.GetService<T>();
    }

    /// <summary>
    /// Gets the SQL Server connection string being used.
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// Gets the SQL Server data seeding options.
    /// </summary>
    public SqlServerDataSeedingOptions Options => _options;

    /// <summary>
    /// Creates and opens a SQL Server connection for long-running operations.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    /// <returns>An opened SQL Server connection</returns>
    public async Task<SqlConnection> CreateAndOpenConnection(CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Executes a SQL command and returns the number of rows affected.
    /// This is a convenience method for simple DDL/DML operations.
    /// </summary>
    /// <param name="sql">The SQL command to execute</param>
    /// <param name="timeout">Command timeout in seconds (optional, uses default from options)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of rows affected</returns>
    public async Task<int> ExecuteCommand(string sql, int? timeout = null, CancellationToken cancellationToken = default)
    {
        using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = timeout ?? _options.CommandTimeoutSeconds;
        
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a SQL query and returns a scalar result.
    /// This is a convenience method for simple queries that return a single value.
    /// </summary>
    /// <param name="sql">The SQL query to execute</param>
    /// <param name="timeout">Command timeout in seconds (optional, uses default from options)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The scalar result, or null if no result</returns>
    public async Task<object?> ExecuteScalar(string sql, int? timeout = null, CancellationToken cancellationToken = default)
    {
        using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = timeout ?? _options.CommandTimeoutSeconds;
        
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a table exists in the SQL Server database.
    /// </summary>
    /// <param name="tableName">The name of the table to check</param>
    /// <param name="schema">The schema name (optional, uses default schema from options)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the table exists, false otherwise</returns>
    public async Task<bool> TableExists(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        schema ??= _options.DefaultSchema;
        
        const string sql = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = @schema 
            AND TABLE_NAME = @tableName";

        using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@schema", schema);
        command.Parameters.AddWithValue("@tableName", tableName);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Checks if a schema exists in the SQL Server database.
    /// </summary>
    /// <param name="schemaName">The name of the schema to check</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the schema exists, false otherwise</returns>
    public async Task<bool> SchemaExists(string schemaName, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.SCHEMATA 
            WHERE SCHEMA_NAME = @schemaName";

        using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@schemaName", schemaName);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Creates a schema in the SQL Server database if it doesn't exist.
    /// </summary>
    /// <param name="schemaName">The name of the schema to create</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    public async Task CreateSchemaIfNotExists(string schemaName, CancellationToken cancellationToken = default)
    {
        if (await SchemaExists(schemaName, cancellationToken))
            return;

        var sql = $"CREATE SCHEMA [{schemaName}]";
        await ExecuteCommand(sql, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the count of rows in a specified table.
    /// </summary>
    /// <param name="tableName">The name of the table to count</param>
    /// <param name="schema">The schema name (optional, uses default schema from options)</param>
    /// <param name="whereClause">Optional WHERE clause (without the WHERE keyword)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of rows in the table</returns>
    public async Task<long> GetRowCount(string tableName, string? schema = null, string? whereClause = null, CancellationToken cancellationToken = default)
    {
        schema ??= _options.DefaultSchema;
        
        var sql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }

        using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    /// <summary>
    /// Executes a command within a transaction scope.
    /// </summary>
    /// <param name="action">The action to execute within the transaction</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    public async Task ExecuteInTransaction(Func<SqlConnection, SqlTransaction, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (!_options.UseTransactions)
        {
            // Execute without transaction
            using var conn = (SqlConnection)GetConnection();
            await conn.OpenAsync(cancellationToken);
            await action(conn, null!, cancellationToken);
            return;
        }

        using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync(cancellationToken);
        
        using var transaction = connection.BeginTransaction(_options.IsolationLevel);
        try
        {
            await action(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Gets SQL Server version information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>SQL Server version string</returns>
    public async Task<string> GetServerVersion(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalar("SELECT @@VERSION", cancellationToken: cancellationToken);
        return result?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Gets the current database name.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Current database name</returns>
    public async Task<string> GetDatabaseName(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalar("SELECT DB_NAME()", cancellationToken: cancellationToken);
        return result?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Protected implementation of dispose pattern for SQL Server resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}
