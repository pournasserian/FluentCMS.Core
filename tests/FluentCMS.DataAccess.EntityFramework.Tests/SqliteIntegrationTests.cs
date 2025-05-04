using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework.Tests.Infrastructure;
using FluentCMS.DataAccess.EntityFramework.Tests.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework.Tests
{
    public class SqliteIntegrationTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TestDbContext> _contextOptions;
        private readonly TestDbContext _context;
        private readonly IApplicationExecutionContext _executionContext;
        private readonly EntityRepository<TestEntity> _entityRepository;
        private readonly AuditableEntityRepository<AuditableTestEntity> _auditableRepository;

        public SqliteIntegrationTests()
        {
            // Create a temporary file for the SQLite database
            _dbPath = Path.Combine(Path.GetTempPath(), $"fluentcms_test_{Guid.NewGuid()}.db");

            // Create SQLite connection
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();

            // Configure the DbContext to use SQLite
            _contextOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create and seed the database
            _context = new TestDbContext(_contextOptions);
            _context.Database.EnsureCreated();

            // Create execution context
            _executionContext = new TestApplicationExecutionContext("integration-test-user");

            // Create repositories
            _entityRepository = new EntityRepository<TestEntity>(_context);
            _auditableRepository = new AuditableEntityRepository<AuditableTestEntity>(_context, _executionContext);
        }

        [Fact]
        public async Task PersistenceTest_DataSurvivesContextDisposal()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "Persistence Test Entity",
                Description = "Testing persistence across context instances",
                Value = 42
            };

            // Act 1 - Add entity and save with first context
            var addedEntity = await _entityRepository.Add(entity);
            await _context.SaveChangesAsync();
            var entityId = addedEntity.Id;

            // Dispose first context
            _context.Dispose();

            // Create a new context with the same database
            using var newConnection = new SqliteConnection($"Data Source={_dbPath}");
            newConnection.Open();

            var newContextOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(newConnection)
                .Options;

            using var newContext = new TestDbContext(newContextOptions);
            var newRepository = new EntityRepository<TestEntity>(newContext);

            // Act 2 - Retrieve entity with new context
            var retrievedEntity = await newRepository.GetById(entityId);

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.Equal(entity.Name, retrievedEntity.Name);
            Assert.Equal(entity.Description, retrievedEntity.Description);
            Assert.Equal(entity.Value, retrievedEntity.Value);
        }

        [Fact]
        public async Task CompleteWorkflow_MultipleOperations()
        {
            // This test simulates a complete workflow with multiple operations

            // 1. Add an entity
            var entity = new AuditableTestEntity
            {
                Name = "Initial Entity",
                Description = "Initial description",
                Value = 1
            };

            var addedEntity = await _auditableRepository.Add(entity);
            await _context.SaveChangesAsync();

            Assert.NotNull(addedEntity);
            Assert.NotEqual(Guid.Empty, addedEntity.Id);
            Assert.Equal("integration-test-user", addedEntity.CreatedBy);

            // 2. Update the entity
            addedEntity.Name = "Updated Entity";
            addedEntity.Value = 2;

            var updatedEntity = await _auditableRepository.Update(addedEntity);
            await _context.SaveChangesAsync();

            Assert.NotNull(updatedEntity);
            Assert.Equal("Updated Entity", updatedEntity.Name);
            Assert.Equal(2, updatedEntity.Value);
            Assert.Equal("integration-test-user", updatedEntity.UpdatedBy);
            Assert.NotNull(updatedEntity.UpdatedAt);

            // 3. Add another entity
            var entity2 = new AuditableTestEntity
            {
                Name = "Second Entity",
                Description = "Second description",
                Value = 3
            };

            await _auditableRepository.Add(entity2);
            await _context.SaveChangesAsync();

            // 4. Query entities
            var allEntities = await _auditableRepository.GetAll();
            Assert.Equal(2, allEntities.Count());

            var filteredEntities = await _auditableRepository.Find(e => e.Value > 1);
            Assert.Equal(2, filteredEntities.Count());

            var countResult = await _auditableRepository.Count(e => e.Name.Contains("Entity"));
            Assert.Equal(2, countResult);

            // 5. Remove the first entity
            var removedEntity = await _auditableRepository.Remove(addedEntity);
            await _context.SaveChangesAsync();

            Assert.NotNull(removedEntity);
            Assert.Equal(addedEntity.Id, removedEntity.Id);

            // 6. Verify removal
            var remainingEntities = await _auditableRepository.GetAll();
            Assert.Single(remainingEntities);
            Assert.Equal("Second Entity", remainingEntities.First().Name);

            var searchResult = await _auditableRepository.GetById(addedEntity.Id);
            Assert.Null(searchResult);
        }

        [Fact]
        public async Task Transaction_ShouldRollbackOnException()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "Transaction Test Entity",
                Description = "Testing transaction rollback",
                Value = 100
            };

            await _entityRepository.Add(entity);
            await _context.SaveChangesAsync();

            // Start a transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Make a valid change
                entity.Value = 200;
                await _entityRepository.Update(entity);
                await _context.SaveChangesAsync();

                // Verify the change is visible within the transaction
                var updatedEntity = await _entityRepository.GetById(entity.Id);
                Assert.Equal(200, updatedEntity!.Value);

                // Force an exception to cause rollback
                throw new Exception("Simulated error to force rollback");

                // This would normally be a commit, but we won't reach here
                // transaction.Commit();
            }
            catch
            {
                // Transaction should automatically roll back when we exit the using block
            }

            // Create a new context to check the database state
            _context.Dispose();

            using var newConnection = new SqliteConnection($"Data Source={_dbPath}");
            newConnection.Open();

            var newContextOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(newConnection)
                .Options;

            using var newContext = new TestDbContext(newContextOptions);
            var newRepository = new EntityRepository<TestEntity>(newContext);

            // Verify the change was rolled back
            var retrievedEntity = await newRepository.GetById(entity.Id);
            Assert.NotNull(retrievedEntity);
            Assert.Equal(100, retrievedEntity.Value); // Should still be 100, not 200
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();

            // Clean up the SQLite database file
            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }
}
