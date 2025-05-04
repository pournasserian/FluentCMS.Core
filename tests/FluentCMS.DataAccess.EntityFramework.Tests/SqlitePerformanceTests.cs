using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework.Sqlite;
using FluentCMS.DataAccess.EntityFramework.Tests.Infrastructure;
using FluentCMS.DataAccess.EntityFramework.Tests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace FluentCMS.DataAccess.EntityFramework.Tests
{
    public class SqlitePerformanceTests : IDisposable
    {
        private readonly string _dbPath;
        //private readonly SqliteConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private readonly TestDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEntityRepository<TestEntity> _entityRepository;

        public SqlitePerformanceTests()
        {
            // Create a temporary file for the SQLite database
            _dbPath = Path.Combine(Path.GetTempPath(), $"fluentcms_test_perf_{Guid.NewGuid()}.db");

            // Create SQLite connection
            var connectionString = $"Data Source={_dbPath}";
            //_connection = new SqliteConnection(connectionString);
            //_connection.Open();

            // Setup services
            var services = new ServiceCollection();
            services.AddSqliteDataAccess<TestDbContext>(connectionString);
            services.AddSingleton<IApplicationExecutionContext>(new TestApplicationExecutionContext());

            // Register TestDbContext as DbContext for UnitOfWork
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

            _serviceProvider = services.BuildServiceProvider();

            // Get context and repositories
            _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
            _dbContext.Database.EnsureCreated();

            _unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
            _entityRepository = _unitOfWork.Repository<IEntityRepository<TestEntity>>();
        }

        [Fact]
        public async Task SqliteBulkInsertPerformance()
        {
            // Skip this test in CI environments or when running all tests
            // This is meant for local performance testing only
            if (IsRunningInCIEnvironment())
            {
                return;
            }

            // Arrange
            const int numberOfEntities = 1000;
            var entities = new List<TestEntity>();

            for (int i = 0; i < numberOfEntities; i++)
            {
                entities.Add(new TestEntity
                {
                    Name = $"Performance Test Entity {i}",
                    Description = $"Description for entity {i}",
                    Value = i
                });
            }

            // Act - Measure time to insert entities
            var stopwatch = Stopwatch.StartNew();

            await _entityRepository.AddMany(entities);
            await _unitOfWork.SaveChanges();

            stopwatch.Stop();

            // Assert - This is not a strict assertion, but a benchmark
            // For SQLite with 1000 entities, we expect it to complete in a reasonable time
            // The actual time will vary by machine, but it gives us a baseline
            Assert.True(stopwatch.ElapsedMilliseconds > 0);

            // Output performance data for information (not an actual test assertion)
            Output.WriteLine($"Inserted {numberOfEntities} entities in {stopwatch.ElapsedMilliseconds}ms");
            Output.WriteLine($"Average time per entity: {stopwatch.ElapsedMilliseconds / (double)numberOfEntities}ms");

            // Verify all entities were inserted
            var count = await _entityRepository.Count();
            Assert.Equal(numberOfEntities, count);
        }

        [Fact]
        public async Task SqliteQueryPerformance()
        {
            // Skip this test in CI environments or when running all tests
            if (IsRunningInCIEnvironment())
            {
                return;
            }

            // Arrange - Insert test data
            const int numberOfEntities = 1000;
            var entities = new List<TestEntity>();

            for (int i = 0; i < numberOfEntities; i++)
            {
                entities.Add(new TestEntity
                {
                    Name = $"Query Test Entity {i}",
                    Description = $"Description for entity {i}",
                    Value = i % 10 // Create groups of values for filtering
                });
            }

            await _entityRepository.AddMany(entities);
            await _unitOfWork.SaveChanges();

            // Act & Assert - Test different query scenarios

            // 1. GetAll performance
            var getAllStopwatch = Stopwatch.StartNew();
            var allEntities = await _entityRepository.GetAll();
            getAllStopwatch.Stop();

            Assert.Equal(numberOfEntities, allEntities.Count());
            Output.WriteLine($"GetAll query time for {numberOfEntities} entities: {getAllStopwatch.ElapsedMilliseconds}ms");

            // 2. Find with filter performance
            var findStopwatch = Stopwatch.StartNew();
            var filteredEntities = await _entityRepository.Find(e => e.Value == 5);
            findStopwatch.Stop();

            Assert.Equal(numberOfEntities / 10, filteredEntities.Count()); // Should be ~100 entities with Value = 5
            Output.WriteLine($"Find query time for filtered entities: {findStopwatch.ElapsedMilliseconds}ms");

            // 3. GetById performance (single entity lookup)
            var firstEntity = allEntities.First();
            var getByIdStopwatch = Stopwatch.StartNew();
            var foundEntity = await _entityRepository.GetById(firstEntity.Id);
            getByIdStopwatch.Stop();

            Assert.NotNull(foundEntity);
            Assert.Equal(firstEntity.Id, foundEntity!.Id);
            Output.WriteLine($"GetById query time: {getByIdStopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task SqliteConcurrencyTest()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "Concurrency Test Entity",
                Description = "Testing SQLite concurrency behavior",
                Value = 100
            };

            // Add and save the entity
            await _entityRepository.Add(entity);
            await _unitOfWork.SaveChanges();

            // Create a second context to simulate concurrent access
            var secondConnectionString = $"Data Source={_dbPath}";
            var secondServices = new ServiceCollection();
            secondServices.AddSqliteDataAccess<TestDbContext>(secondConnectionString);
            secondServices.AddSingleton<IApplicationExecutionContext>(new TestApplicationExecutionContext());

            // Register TestDbContext as DbContext for UnitOfWork
            secondServices.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

            var secondServiceProvider = secondServices.BuildServiceProvider();
            var secondContext = secondServiceProvider.GetRequiredService<TestDbContext>();
            var secondUnitOfWork = secondServiceProvider.GetRequiredService<IUnitOfWork>();
            var secondRepository = secondUnitOfWork.Repository<IEntityRepository<TestEntity>>();

            // Act - Load the same entity in both contexts
            var firstContextEntity = await _entityRepository.GetById(entity.Id);
            var secondContextEntity = await secondRepository.GetById(entity.Id);

            Assert.NotNull(firstContextEntity);
            Assert.NotNull(secondContextEntity);

            // Modify the entity in both contexts
            firstContextEntity!.Value = 200;
            secondContextEntity!.Value = 300;

            // Save the first context's changes
            await _entityRepository.Update(firstContextEntity);
            await _unitOfWork.SaveChanges();

            // Now save the second context's changes - SQLite doesn't have built-in optimistic concurrency
            // so this won't fail, but we should see the "last write wins" behavior
            await secondRepository.Update(secondContextEntity);
            await secondUnitOfWork.SaveChanges();

            // Assert - Check what value was ultimately saved
            // Create a third context to get a fresh view
            var thirdConnectionString = $"Data Source={_dbPath}";
            var thirdServices = new ServiceCollection();
            thirdServices.AddSqliteDataAccess<TestDbContext>(thirdConnectionString);

            // Register TestDbContext as DbContext for UnitOfWork
            thirdServices.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

            var thirdServiceProvider = thirdServices.BuildServiceProvider();
            var thirdContext = thirdServiceProvider.GetRequiredService<TestDbContext>();

            var finalEntity = await thirdContext.TestEntities.FindAsync(entity.Id);
            Assert.NotNull(finalEntity);

            // In SQLite with no concurrency detection, the last write wins
            Assert.Equal(300, finalEntity!.Value);
        }

        private bool IsRunningInCIEnvironment()
        {
            // This is a simple check for CI environment
            // You can expand this based on your actual CI system
            return Environment.GetEnvironmentVariable("CI") != null;
        }

        private ITestOutputHelper Output =>
            new TestOutputHelper();

        public void Dispose()
        {
            //_connection.Dispose();

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

        // Simple test output helper for use in tests
        private class TestOutputHelper : ITestOutputHelper
        {
            public void WriteLine(string message)
            {
                // In a real test runner, this would output to the test log
                // For our purposes, we'll just write to console
                Console.WriteLine(message);
            }

            public void WriteLine(string format, params object[] args)
            {
                Console.WriteLine(format, args);
            }
        }
    }

    // Interface needed for the TestOutputHelper
    public interface ITestOutputHelper
    {
        void WriteLine(string message);
        void WriteLine(string format, params object[] args);
    }
}
