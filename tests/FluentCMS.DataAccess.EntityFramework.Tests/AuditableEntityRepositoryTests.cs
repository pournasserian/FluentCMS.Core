using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework.Tests.Infrastructure;
using FluentCMS.DataAccess.EntityFramework.Tests.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework.Tests
{
    public class AuditableEntityRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TestDbContext> _contextOptions;
        private readonly TestDbContext _context;
        private readonly IApplicationExecutionContext _executionContext;
        private readonly AuditableEntityRepository<AuditableTestEntity> _repository;

        public AuditableEntityRepositoryTests()
        {
            // Create and open an in-memory SQLite database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Configure the DbContext to use SQLite
            _contextOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create and seed the database
            _context = new TestDbContext(_contextOptions);
            _context.Database.EnsureCreated();

            // Create execution context with a test user
            _executionContext = new TestApplicationExecutionContext("test-user");

            // Create repository
            _repository = new AuditableEntityRepository<AuditableTestEntity>(_context, _executionContext);
        }

        [Fact]
        public async Task Add_ShouldSetCreatedAtAndCreatedBy()
        {
            // Arrange
            var entity = new AuditableTestEntity
            {
                Name = "Test Entity",
                Description = "Test Description",
                Value = 42
            };

            // Act
            var result = await _repository.Add(entity);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-user", result.CreatedBy);
            Assert.NotEqual(default, result.CreatedAt);
            Assert.Equal(DateTime.UtcNow.Date, result.CreatedAt.Date);
            Assert.Equal(0, result.Version); // Version is not set in Add, but initialized in AddMany
            Assert.Null(result.UpdatedBy);
            Assert.Null(result.UpdatedAt);

            var dbEntity = await _context.AuditableTestEntities.FindAsync(result.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal("test-user", dbEntity.CreatedBy);
            Assert.NotEqual(default, dbEntity.CreatedAt);
            Assert.Null(dbEntity.UpdatedBy);
            Assert.Null(dbEntity.UpdatedAt);
        }

        [Fact]
        public async Task AddMany_ShouldSetCreatedAtCreatedByAndVersionForAllEntities()
        {
            // Arrange
            var entities = new List<AuditableTestEntity>
            {
                new() { Name = "Entity 1", Description = "Description 1", Value = 1 },
                new() { Name = "Entity 2", Description = "Description 2", Value = 2 },
                new() { Name = "Entity 3", Description = "Description 3", Value = 3 }
            };

            // Act
            var results = await _repository.AddMany(entities);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(entities.Count, results.Count());

            foreach (var result in results)
            {
                Assert.Equal("test-user", result.CreatedBy);
                Assert.NotEqual(default, result.CreatedAt);
                Assert.Equal(DateTime.UtcNow.Date, result.CreatedAt.Date);
                Assert.Equal(1, result.Version);
                Assert.Null(result.UpdatedBy);
                Assert.Null(result.UpdatedAt);

                var dbEntity = await _context.AuditableTestEntities.FindAsync(result.Id);
                Assert.NotNull(dbEntity);
                Assert.Equal("test-user", dbEntity.CreatedBy);
                Assert.NotEqual(default, dbEntity.CreatedAt);
                Assert.Equal(1, dbEntity.Version);
                Assert.Null(dbEntity.UpdatedBy);
                Assert.Null(dbEntity.UpdatedAt);
            }
        }

        [Fact]
        public async Task Update_ShouldSetUpdatedAtAndUpdatedByAndIncrementVersion()
        {
            // Arrange
            var entity = new AuditableTestEntity
            {
                Name = "Original Name",
                Description = "Original Description",
                Value = 10
            };

            // Add the entity first
            await _repository.Add(entity);
            await _context.SaveChangesAsync();

            // Reload from database to ensure we have the initial state with CreatedAt/CreatedBy set
            entity = await _context.AuditableTestEntities.FindAsync(entity.Id) ??
                throw new Exception("Entity was not saved properly.");

            var initialVersion = entity.Version;
            var createdAt = entity.CreatedAt;
            var createdBy = entity.CreatedBy;

            // Modify the entity
            entity.Name = "Updated Name";
            entity.Description = "Updated Description";
            entity.Value = 20;

            // Act
            var result = await _repository.Update(entity);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("test-user", result.UpdatedBy);
            Assert.NotEqual(default, result.UpdatedAt);
            Assert.Equal(DateTime.UtcNow.Date, result.UpdatedAt!.Value.Date);
            Assert.Equal(initialVersion + 1, result.Version);

            // CreatedAt and CreatedBy should remain unchanged
            Assert.Equal(createdAt, result.CreatedAt);
            Assert.Equal(createdBy, result.CreatedBy);

            var dbEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal("test-user", dbEntity.UpdatedBy);
            Assert.NotEqual(default, dbEntity.UpdatedAt);
            Assert.Equal(initialVersion + 1, dbEntity.Version);
            Assert.Equal(createdAt, dbEntity.CreatedAt);
            Assert.Equal(createdBy, dbEntity.CreatedBy);
        }

        [Fact]
        public async Task Update_ShouldContinueToIncrementVersionWithEachUpdate()
        {
            // Arrange
            var entity = new AuditableTestEntity
            {
                Name = "Initial Name",
                Description = "Initial Description",
                Value = 5
            };

            // Add the entity first
            await _repository.Add(entity);
            await _context.SaveChangesAsync();

            // First update
            entity = await _context.AuditableTestEntities.FindAsync(entity.Id) ??
                throw new Exception("Entity was not saved properly.");
            entity.Name = "First Update";
            await _repository.Update(entity);
            await _context.SaveChangesAsync();

            // Second update
            entity = await _context.AuditableTestEntities.FindAsync(entity.Id) ??
                throw new Exception("Entity was not saved properly.");
            entity.Name = "Second Update";
            await _repository.Update(entity);
            await _context.SaveChangesAsync();

            // Third update
            entity = await _context.AuditableTestEntities.FindAsync(entity.Id) ??
                throw new Exception("Entity was not saved properly.");
            entity.Name = "Third Update";

            // Act
            var result = await _repository.Update(entity);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Version); // Should be 3 after 3 updates

            var dbEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal(3, dbEntity.Version);
        }

        [Fact]
        public async Task DifferentUsers_ShouldBeReflectedInAuditFields()
        {
            // Arrange - Create a new repository with a different user
            var user1Context = new TestApplicationExecutionContext("user-1");
            var user2Context = new TestApplicationExecutionContext("user-2");

            var user1Repository = new AuditableEntityRepository<AuditableTestEntity>(_context, user1Context);
            var user2Repository = new AuditableEntityRepository<AuditableTestEntity>(_context, user2Context);

            var entity = new AuditableTestEntity
            {
                Name = "Multi-user Entity",
                Description = "Entity for testing multi-user auditing",
                Value = 100
            };

            // Act - User 1 creates the entity
            var result1 = await user1Repository.Add(entity);
            await _context.SaveChangesAsync();

            // User 2 updates the entity
            entity = await _context.AuditableTestEntities.FindAsync(entity.Id) ??
                throw new Exception("Entity was not saved properly.");
            entity.Description = "Updated by User 2";
            var result2 = await user2Repository.Update(entity);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result2);
            Assert.Equal("user-1", result2.CreatedBy);
            Assert.Equal("user-2", result2.UpdatedBy);

            var dbEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal("user-1", dbEntity.CreatedBy);
            Assert.Equal("user-2", dbEntity.UpdatedBy);
        }

        [Fact]
        public async Task Operations_ShouldRespectCancellationToken()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel the token immediately

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Add(new AuditableTestEntity { Name = "Test" }, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.AddMany(new[] { new AuditableTestEntity { Name = "Test" } }, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Update(new AuditableTestEntity { Name = "Test" }, cts.Token));
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }
    }
}
