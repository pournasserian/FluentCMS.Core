using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework.Tests.Infrastructure;
using FluentCMS.DataAccess.EntityFramework.Tests.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework.Tests
{
    public class AuditingSaveChangesInterceptorTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TestDbContext> _contextOptions;
        private readonly TestDbContext _context;
        private readonly IApplicationExecutionContext _executionContext;

        public AuditingSaveChangesInterceptorTests()
        {
            // Create and open an in-memory SQLite database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Create execution context with a test user
            _executionContext = new TestApplicationExecutionContext("test-user");

            // Configure the DbContext to use SQLite and the audit interceptor
            _contextOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(new EventBusSaveChangesInterceptor(_executionContext))
                .Options;

            // Create and seed the database
            _context = new TestDbContext(_contextOptions);
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task Interceptor_ShouldSetAuditPropertiesOnAdd()
        {
            // Arrange
            var entity = new AuditableTestEntity
            {
                Name = "Test Entity",
                Description = "Test Description",
                Value = 42
            };

            // Act - Add directly to DbContext without using repository
            await _context.AuditableTestEntities.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Assert
            var savedEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(savedEntity);
            Assert.Equal("test-user", savedEntity.CreatedBy);
            Assert.NotEqual(default, savedEntity.CreatedAt);
            Assert.Equal(DateTime.UtcNow.Date, savedEntity.CreatedAt.Date);
            Assert.Equal(1, savedEntity.Version);
            Assert.Null(savedEntity.UpdatedBy);
            Assert.Null(savedEntity.UpdatedAt);
        }

        [Fact]
        public async Task Interceptor_ShouldSetAuditPropertiesOnUpdate()
        {
            // Arrange - First add an entity
            var entity = new AuditableTestEntity
            {
                Name = "Original Name",
                Description = "Original Description",
                Value = 10
            };

            await _context.AuditableTestEntities.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act - Update the entity directly through DbContext
            var savedEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(savedEntity);
            
            // Change the entity and save
            savedEntity.Name = "Updated Name";
            _context.Entry(savedEntity).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Assert
            var updatedEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(updatedEntity);
            Assert.Equal("Updated Name", updatedEntity.Name);
            Assert.Equal("test-user", updatedEntity.CreatedBy);
            Assert.Equal("test-user", updatedEntity.UpdatedBy);
            Assert.NotEqual(default, updatedEntity.CreatedAt);
            Assert.NotNull(updatedEntity.UpdatedAt);
            Assert.Equal(DateTime.UtcNow.Date, updatedEntity.UpdatedAt!.Value.Date);
            Assert.Equal(2, updatedEntity.Version); // Should be 2 after the update
        }

        [Fact]
        public async Task Interceptor_ShouldTrackMultipleVersionChanges()
        {
            // Arrange
            var entity = new AuditableTestEntity
            {
                Name = "Initial Name",
                Description = "Initial Description",
                Value = 5
            };

            // Act - Add and make several updates
            await _context.AuditableTestEntities.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Update 1
            var savedEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(savedEntity);
            savedEntity.Name = "First Update";
            await _context.SaveChangesAsync();

            // Update 2
            savedEntity.Name = "Second Update";
            await _context.SaveChangesAsync();

            // Update 3
            savedEntity.Name = "Third Update";
            await _context.SaveChangesAsync();

            // Assert
            var finalEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(finalEntity);
            Assert.Equal("Third Update", finalEntity.Name);
            Assert.Equal(4, finalEntity.Version); // Should be 4 after initial save + 3 updates
            Assert.Equal("test-user", finalEntity.CreatedBy);
            Assert.Equal("test-user", finalEntity.UpdatedBy);
        }

        [Fact]
        public async Task Interceptor_ShouldHandleDifferentUsers()
        {
            // Arrange - First add an entity as user1
            var entity = new AuditableTestEntity
            {
                Name = "Multi-user Entity",
                Description = "Entity for testing multi-user auditing",
                Value = 100
            };

            await _context.AuditableTestEntities.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Change to user2 for the update
            var user2Context = new TestApplicationExecutionContext("user-2");
            var user2Interceptor = new EventBusSaveChangesInterceptor(user2Context);
            
            var user2ContextOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(user2Interceptor)
                .Options;
            
            using var user2DbContext = new TestDbContext(user2ContextOptions);

            // Act - Update as user2
            var savedEntity = await user2DbContext.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(savedEntity);
            savedEntity.Description = "Updated by User 2";
            await user2DbContext.SaveChangesAsync();

            // Assert
            var finalEntity = await _context.AuditableTestEntities.FindAsync(entity.Id);
            Assert.NotNull(finalEntity);
            Assert.Equal("test-user", finalEntity.CreatedBy);
            Assert.Equal("user-2", finalEntity.UpdatedBy);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }
    }
}
