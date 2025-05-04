using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework.Sqlite;
using FluentCMS.DataAccess.EntityFramework.Tests.Infrastructure;
using FluentCMS.DataAccess.EntityFramework.Tests.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataAccess.EntityFramework.Tests
{
    public class SqliteProviderTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _connection;

        public SqliteProviderTests()
        {
            // Create a temporary file for the SQLite database
            _dbPath = Path.Combine(Path.GetTempPath(), $"fluentcms_test_provider_{Guid.NewGuid()}.db");
            
            // Create SQLite connection string
            var connectionString = $"Data Source={_dbPath}";
            
            // Create and open connection
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
        }

        [Fact]
        public void AddSqliteDataAccess_ShouldRegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = $"Data Source={_dbPath}";

            // Act
            services.AddSqliteDataAccess<TestDbContext>(connectionString);
            
            // Register TestDbContext as DbContext for UnitOfWork
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());
            
            // Register IApplicationExecutionContext for AuditableEntityRepository
            services.AddSingleton<IApplicationExecutionContext>(new TestApplicationExecutionContext());
            
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var dbContext = serviceProvider.GetService<TestDbContext>();
            Assert.NotNull(dbContext);

            var dbContextOptions = serviceProvider.GetService<DbContextOptions<TestDbContext>>();
            Assert.NotNull(dbContextOptions);
            
            // Verify options are configured for SQLite
            var extension = dbContextOptions!.Extensions.FirstOrDefault(e => e.GetType().Name.Contains("Sqlite"));
            Assert.NotNull(extension);

            // Verify repositories are registered
            var repository = serviceProvider.GetService<IRepository<TestEntity>>();
            Assert.NotNull(repository);
            Assert.IsType<Repository<TestEntity>>(repository);

            var entityRepository = serviceProvider.GetService<IEntityRepository<TestEntity>>();
            Assert.NotNull(entityRepository);
            Assert.IsType<EntityRepository<TestEntity>>(entityRepository);

            var auditableRepository = serviceProvider.GetService<IAuditableEntityRepository<AuditableTestEntity>>();
            Assert.NotNull(auditableRepository);
            Assert.IsType<AuditableEntityRepository<AuditableTestEntity>>(auditableRepository);

            // Verify unit of work is registered
            var unitOfWork = serviceProvider.GetService<IUnitOfWork>();
            Assert.NotNull(unitOfWork);
            Assert.IsType<UnitOfWork<TestDbContext>>(unitOfWork);
        }

        [Fact]
        public async Task SqliteProvider_ShouldWorkWithTransactions()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = $"Data Source={_dbPath}";
            
            services.AddSqliteDataAccess<TestDbContext>(connectionString);
            services.AddSingleton<IApplicationExecutionContext, TestApplicationExecutionContext>();
            
            // Register TestDbContext as DbContext for UnitOfWork
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());
            
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            
            // Ensure database is created
            dbContext.Database.EnsureCreated();
            
            // Get unit of work from container
            var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            var entityRepository = unitOfWork.Repository<IEntityRepository<TestEntity>>();
            
            // Act - Use a transaction
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            
            var entity = new TestEntity
            {
                Name = "Transaction Test",
                Description = "Testing SQLite transactions",
                Value = 42
            };
            
            // Add entity
            await entityRepository.Add(entity);
            await unitOfWork.SaveChanges();
            
            // Commit transaction
            await transaction.CommitAsync();
            
            // Assert - Entity should be saved in database
            var savedEntity = await dbContext.TestEntities.FirstOrDefaultAsync(e => e.Name == "Transaction Test");
            Assert.NotNull(savedEntity);
            Assert.Equal("Testing SQLite transactions", savedEntity.Description);
            Assert.Equal(42, savedEntity.Value);
        }

        [Fact]
        public async Task SqliteProvider_ShouldSupportRollback()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = $"Data Source={_dbPath}";
            
            services.AddSqliteDataAccess<TestDbContext>(connectionString);
            services.AddSingleton<IApplicationExecutionContext, TestApplicationExecutionContext>();
            
            // Register TestDbContext as DbContext for UnitOfWork
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());
            
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            
            // Ensure database is created
            dbContext.Database.EnsureCreated();
            
            // Get unit of work from container
            var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            var entityRepository = unitOfWork.Repository<IEntityRepository<TestEntity>>();
            
            // Act - Use a transaction but don't commit
            using (var transaction = await dbContext.Database.BeginTransactionAsync())
            {
                var entity = new TestEntity
                {
                    Name = "Rollback Test",
                    Description = "This entity should be rolled back",
                    Value = 999
                };
                
                // Add entity
                await entityRepository.Add(entity);
                await unitOfWork.SaveChanges();
                
                // Don't commit - transaction will be rolled back when disposed
            }
            
            // Create new context to ensure we're not reading from EF cache
            var newServices = new ServiceCollection();
            newServices.AddSqliteDataAccess<TestDbContext>(connectionString);
            
            // Register TestDbContext as DbContext for UnitOfWork
            newServices.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());
            
            var newProvider = newServices.BuildServiceProvider();
            var newContext = newProvider.GetRequiredService<TestDbContext>();
            
            // Assert - Entity should not be in database
            var retrievedEntity = await newContext.TestEntities.FirstOrDefaultAsync(e => e.Name == "Rollback Test");
            Assert.Null(retrievedEntity);
        }

        [Fact]
        public async Task SqliteProvider_ShouldSupportMultipleOperationsInSameTransaction()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = $"Data Source={_dbPath}";
            
            services.AddSqliteDataAccess<TestDbContext>(connectionString);
            services.AddSingleton<IApplicationExecutionContext, TestApplicationExecutionContext>();
            
            // Register TestDbContext as DbContext for UnitOfWork
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());
            
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            
            // Ensure database is created
            dbContext.Database.EnsureCreated();
            
            // Get unit of work from container
            var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            var entityRepository = unitOfWork.Repository<IEntityRepository<TestEntity>>();
            var auditableRepository = unitOfWork.Repository<IAuditableEntityRepository<AuditableTestEntity>>();
            
            // Act - Use a transaction for multiple operations
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            
            // Add regular entity
            var entity1 = new TestEntity { Name = "Regular Entity", Value = 1 };
            await entityRepository.Add(entity1);
            
            // Add auditable entity
            var entity2 = new AuditableTestEntity { Name = "Auditable Entity", Value = 2 };
            await auditableRepository.Add(entity2);
            
            // Save all changes
            await unitOfWork.SaveChanges();
            
            // Commit transaction
            await transaction.CommitAsync();
            
            // Assert - Both entities should be saved
            var savedEntity1 = await dbContext.TestEntities.FirstOrDefaultAsync(e => e.Name == "Regular Entity");
            var savedEntity2 = await dbContext.AuditableTestEntities.FirstOrDefaultAsync(e => e.Name == "Auditable Entity");
            
            Assert.NotNull(savedEntity1);
            Assert.Equal(1, savedEntity1.Value);
            
            Assert.NotNull(savedEntity2);
            Assert.Equal(2, savedEntity2.Value);
            Assert.NotNull(savedEntity2.CreatedBy);
            Assert.NotEqual(default, savedEntity2.CreatedAt);
        }

        public void Dispose()
        {
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
