using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataSeeding.Sqlite.Context;

/// <summary>
/// SQLite-specific implementation of SeedingContext.
/// Provides database connection management and service access for SQLite databases.
/// </summary>
public class SqliteSeedingContext : SeedingContext
{
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;
    private SqliteConnection? _connection;

    /// <summary>
    /// Initializes a new instance of SqliteSeedingContext with connection string and service provider.
    /// </summary>
    /// <param name="connectionString">SQLite connection string</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    public SqliteSeedingContext(string connectionString, IServiceProvider serviceProvider)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets a SQLite database connection.
    /// Creates a new connection on each call to ensure thread safety and proper resource management.
    /// </summary>
    /// <returns>A SQLite database connection</returns>
    public override IDbConnection GetConnection()
    {
        return new SqliteConnection(_connectionString);
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
    /// Gets the SQLite connection string being used.
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// Creates and opens a SQLite connection for long-running operations.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    /// <returns>An opened SQLite connection</returns>
    public async Task<SqliteConnection> CreateAndOpenConnection(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Executes a SQL command and returns the number of rows affected.
    /// This is a convenience method for simple DDL/DML operations.
    /// </summary>
    /// <param name="sql">The SQL command to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of rows affected</returns>
    public async Task<int> ExecuteCommand(string sql, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a SQL query and returns a scalar result.
    /// This is a convenience method for simple queries that return a single value.
    /// </summary>
    /// <param name="sql">The SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The scalar result, or null if no result</returns>
    public async Task<object?> ExecuteScalar(string sql, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a table exists in the SQLite database.
    /// </summary>
    /// <param name="tableName">The name of the table to check</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the table exists, false otherwise</returns>
    public async Task<bool> TableExists(string tableName, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM sqlite_master 
            WHERE type = 'table' AND name = @tableName";

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@tableName", tableName);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Gets the count of rows in a specified table.
    /// </summary>
    /// <param name="tableName">The name of the table to count</param>
    /// <param name="whereClause">Optional WHERE clause (without the WHERE keyword)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of rows in the table</returns>
    public async Task<long> GetRowCount(string tableName, string? whereClause = null, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT COUNT(*) FROM {tableName}";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    /// <summary>
    /// Protected implementation of dispose pattern for SQLite resources.
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
