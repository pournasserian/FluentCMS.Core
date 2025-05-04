using FluentCMS.DataAccess.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FluentCMS.DataAccess.EntityFramework.Tests;

public class AuditableEntitySaveChangesInterceptorTests
{
    // A simple entity for testing
    private class TestAuditableEntity : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    // Test DbContext
    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestAuditableEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAuditableEntity>();
            base.OnModelCreating(modelBuilder);
        }
    }

    // Mock user context for testing
    private class TestExecutionContext : IApplicationExecutionContext
    {
        public bool IsAuthenticated { get; set; }
        public string Language { get; set; } = "en-US";
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public string TraceId { get; set; } = Guid.NewGuid().ToString();
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();
        public Guid? UserId { get; set; }
        public string UserIp { get; set; } = "127.0.0.1";
        public string Username { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Should_Set_Audit_Properties_On_Add()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Configure a test execution context with a known username
        var executionContext = new TestExecutionContext
        {
            IsAuthenticated = true,
            Username = "test-user",
            UserId = Guid.NewGuid()
        };
        
        services.AddSingleton<IApplicationExecutionContext>(executionContext);
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        
        // Create SQLite in-memory connection
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        // Configure the SQLite database with our interceptor
        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            options.UseSqlite(connection);
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Create a new entity to test with
        var entity = new TestAuditableEntity { Name = "Test Entity" };
        
        // Act - Add entity
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            // Create the database
            dbContext.Database.EnsureCreated();
            
            await dbContext.TestEntities.AddAsync(entity);
            await dbContext.SaveChangesAsync();
        }
        
        // Assert - Properties should be set
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var savedEntity = await dbContext.TestEntities.FindAsync(entity.Id);
            
            Assert.NotNull(savedEntity);
            Assert.Equal("Test Entity", savedEntity!.Name);
            
            // Verify audit properties were set
            Assert.Equal("test-user", savedEntity.CreatedBy);
            Assert.Equal(DateTime.UtcNow.Date, savedEntity.CreatedAt.Date);  // Compare dates only to avoid precision issues
            Assert.Null(savedEntity.UpdatedAt);
            Assert.Null(savedEntity.UpdatedBy);
            Assert.Equal(1, savedEntity.Version);
        }
    }

    [Fact]
    public async Task Should_Update_Audit_Properties_On_Modify()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Create SQLite in-memory connection
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        // First, create with one user
        var initialExecutionContext = new TestExecutionContext
        {
            IsAuthenticated = true,
            Username = "creator-user"
        };
        
        services.AddSingleton<IApplicationExecutionContext>(initialExecutionContext);
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        
        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            options.UseSqlite(connection);
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Create a new entity 
        var entity = new TestAuditableEntity { Name = "Initial Name" };
        Guid entityId;
        
        // Act 1 - Add entity with first user
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            // Create the database
            dbContext.Database.EnsureCreated();
            
            await dbContext.TestEntities.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            entityId = entity.Id;
        }
        
        // Replace the execution context with a different user but keep the same connection
        services = new ServiceCollection();
        var updaterExecutionContext = new TestExecutionContext
        {
            IsAuthenticated = true,
            Username = "updater-user"
        };
        
        services.AddSingleton<IApplicationExecutionContext>(updaterExecutionContext);
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        
        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            options.UseSqlite(connection);
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });
        
        serviceProvider = services.BuildServiceProvider();
        
        // Act 2 - Update the entity with second user
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var savedEntity = await dbContext.TestEntities.FindAsync(entityId);
            savedEntity!.Name = "Updated Name";
            await dbContext.SaveChangesAsync();
        }
        
        // Assert - Verify updated audit properties
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var updatedEntity = await dbContext.TestEntities.FindAsync(entityId);
            
            Assert.NotNull(updatedEntity);
            Assert.Equal("Updated Name", updatedEntity!.Name);
            
            // Original creator should be preserved
            Assert.Equal("creator-user", updatedEntity.CreatedBy);
            
            // Update info should reflect the second user
            Assert.Equal("updater-user", updatedEntity.UpdatedBy);
            Assert.NotNull(updatedEntity.UpdatedAt);
            Assert.Equal(DateTime.UtcNow.Date, updatedEntity.UpdatedAt!.Value.Date);
            Assert.Equal(2, updatedEntity.Version);
        }
    }
}
