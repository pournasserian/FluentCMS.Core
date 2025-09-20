using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.DataSeeding.MongoDB.Context;

/// <summary>
/// MongoDB-specific implementation of SeedingContext.
/// Provides database connection management and service access for MongoDB databases.
/// </summary>
public class MongoDbSeedingContext : SeedingContext
{
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly IServiceProvider _serviceProvider;
    private MongoClient? _client;
    private IMongoDatabase? _database;

    /// <summary>
    /// Initializes a new instance of MongoDbSeedingContext with connection string, database name, and service provider.
    /// </summary>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">Name of the MongoDB database</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    public MongoDbSeedingContext(string connectionString, string databaseName, IServiceProvider serviceProvider)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets a database connection. For MongoDB, this returns null as MongoDB uses different connection patterns.
    /// Use GetDatabase() or GetClient() instead for MongoDB operations.
    /// </summary>
    /// <returns>Always returns null for MongoDB</returns>
    public override IDbConnection? GetConnection()
    {
        // MongoDB doesn't use IDbConnection, return null
        // Consumers should use GetDatabase() or GetClient() instead
        return null;
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
    /// Gets the MongoDB connection string being used.
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// Gets the MongoDB database name being used.
    /// </summary>
    public string DatabaseName => _databaseName;

    /// <summary>
    /// Gets the MongoDB client instance. Creates one if it doesn't exist.
    /// </summary>
    /// <returns>MongoDB client instance</returns>
    public MongoClient GetClient()
    {
        return _client ??= new MongoClient(_connectionString);
    }

    /// <summary>
    /// Gets the MongoDB database instance. Creates one if it doesn't exist.
    /// </summary>
    /// <returns>MongoDB database instance</returns>
    public IMongoDatabase GetDatabase()
    {
        return _database ??= GetClient().GetDatabase(_databaseName);
    }

    /// <summary>
    /// Gets a MongoDB collection for the specified type.
    /// </summary>
    /// <typeparam name="T">The document type</typeparam>
    /// <param name="collectionName">The name of the collection</param>
    /// <returns>MongoDB collection instance</returns>
    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return GetDatabase().GetCollection<T>(collectionName);
    }

    /// <summary>
    /// Checks if a collection exists in the MongoDB database.
    /// </summary>
    /// <param name="collectionName">The name of the collection to check</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the collection exists, false otherwise</returns>
    public async Task<bool> CollectionExists(string collectionName, CancellationToken cancellationToken = default)
    {
        var database = GetDatabase();
        var filter = new BsonDocument("name", collectionName);
        var collections = await database.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = filter }, cancellationToken);
        return await collections.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the count of documents in a specified collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to count</param>
    /// <param name="filter">Optional filter to apply when counting documents</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of documents in the collection</returns>
    public async Task<long> GetDocumentCount(string collectionName, FilterDefinition<BsonDocument>? filter = null, CancellationToken cancellationToken = default)
    {
        var collection = GetDatabase().GetCollection<BsonDocument>(collectionName);
        filter ??= Builders<BsonDocument>.Filter.Empty;
        return await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the count of documents in a specified collection with typed filter.
    /// </summary>
    /// <typeparam name="T">The document type</typeparam>
    /// <param name="collectionName">The name of the collection to count</param>
    /// <param name="filter">Optional filter to apply when counting documents</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of documents in the collection</returns>
    public async Task<long> GetDocumentCount<T>(string collectionName, FilterDefinition<T>? filter = null, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection<T>(collectionName);
        filter ??= Builders<T>.Filter.Empty;
        return await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a collection in the MongoDB database.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create</param>
    /// <param name="options">Optional collection creation options</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    public async Task CreateCollection(string collectionName, CreateCollectionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var database = GetDatabase();
        await database.CreateCollectionAsync(collectionName, options, cancellationToken);
    }

    /// <summary>
    /// Creates an index on a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection</param>
    /// <param name="fieldName">The field name to index</param>
    /// <param name="unique">Whether the index should be unique</param>
    /// <param name="ascending">Whether the index should be ascending (true) or descending (false)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The name of the created index</returns>
    public async Task<string> CreateIndex(string collectionName, string fieldName, bool unique = false, bool ascending = true, CancellationToken cancellationToken = default)
    {
        var collection = GetDatabase().GetCollection<BsonDocument>(collectionName);
        
        var indexBuilder = Builders<BsonDocument>.IndexKeys;
        var indexDefinition = ascending 
            ? indexBuilder.Ascending(fieldName) 
            : indexBuilder.Descending(fieldName);
            
        var indexOptions = new CreateIndexOptions { Unique = unique };
        var indexModel = new CreateIndexModel<BsonDocument>(indexDefinition, indexOptions);
        
        return await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a compound index on a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection</param>
    /// <param name="fields">Dictionary of field names and their sort direction (true = ascending, false = descending)</param>
    /// <param name="unique">Whether the index should be unique</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The name of the created index</returns>
    public async Task<string> CreateCompoundIndex(string collectionName, Dictionary<string, bool> fields, bool unique = false, CancellationToken cancellationToken = default)
    {
        var collection = GetDatabase().GetCollection<BsonDocument>(collectionName);
        var indexBuilder = Builders<BsonDocument>.IndexKeys;
        
        IndexKeysDefinition<BsonDocument> indexDefinition = indexBuilder.Combine(
            fields.Select(field => field.Value 
                ? indexBuilder.Ascending(field.Key) 
                : indexBuilder.Descending(field.Key))
        );
        
        var indexOptions = new CreateIndexOptions { Unique = unique };
        var indexModel = new CreateIndexModel<BsonDocument>(indexDefinition, indexOptions);
        
        return await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Drops a collection from the MongoDB database.
    /// </summary>
    /// <param name="collectionName">The name of the collection to drop</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    public async Task DropCollection(string collectionName, CancellationToken cancellationToken = default)
    {
        var database = GetDatabase();
        await database.DropCollectionAsync(collectionName, cancellationToken);
    }

    /// <summary>
    /// Protected implementation of dispose pattern for MongoDB resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // MongoDB client doesn't require explicit disposal in most cases
            // The client is thread-safe and designed to be long-lived
            _client = null;
            _database = null;
        }
        
        base.Dispose(disposing);
    }
}
