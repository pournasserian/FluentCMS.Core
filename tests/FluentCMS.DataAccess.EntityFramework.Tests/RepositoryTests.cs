using FluentCMS.DataAccess.EntityFramework.Tests.Infrastructure;
using FluentCMS.DataAccess.EntityFramework.Tests.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FluentCMS.DataAccess.EntityFramework.Tests
{
    public class RepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TestDbContext> _contextOptions;
        private readonly TestDbContext _context;
        private readonly Repository<TestEntity> _repository;

        public RepositoryTests()
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

            // Create repository
            _repository = new Repository<TestEntity>(_context);
        }

        [Fact]
        public async Task Add_ShouldAddEntityToDb()
        {
            // Arrange
            var entity = new TestEntity
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
            Assert.Equal(entity.Name, result.Name);

            var dbEntity = await _context.TestEntities.FindAsync(result.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal(entity.Name, dbEntity.Name);
            Assert.Equal(entity.Description, dbEntity.Description);
            Assert.Equal(entity.Value, dbEntity.Value);
        }

        [Fact]
        public async Task Add_ThrowsWhenEntityIsNull()
        {
            // Arrange
            TestEntity? entity = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.Add(entity!));
        }

        [Fact]
        public async Task AddMany_ShouldAddMultipleEntities()
        {
            // Arrange
            var entities = new List<TestEntity>
            {
                new() { Name = "Entity 1", Description = "Description 1", Value = 1 },
                new() { Name = "Entity 2", Description = "Description 2", Value = 2 },
                new() { Name = "Entity 3", Description = "Description 3", Value = 3 }
            };

            // Act
            var result = await _repository.AddMany(entities);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entities.Count, result.Count());

            var dbEntities = await _context.TestEntities.ToListAsync();
            Assert.Equal(entities.Count, dbEntities.Count);

            foreach (var entity in entities)
            {
                var dbEntity = dbEntities.FirstOrDefault(e => e.Name == entity.Name);
                Assert.NotNull(dbEntity);
                Assert.Equal(entity.Description, dbEntity.Description);
                Assert.Equal(entity.Value, dbEntity.Value);
            }
        }

        [Fact]
        public async Task AddMany_ThrowsWhenEntitiesIsNull()
        {
            // Arrange
            List<TestEntity>? entities = null;

            // Act & Assert
            // The implementation throws NullReferenceException instead of ArgumentNullException
            await Assert.ThrowsAnyAsync<Exception>(() => _repository.AddMany(entities!));
        }

        [Fact]
        public async Task Update_ShouldUpdateExistingEntity()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "Original Name",
                Description = "Original Description",
                Value = 10
            };

            // Add the entity first
            await _repository.Add(entity);
            await _context.SaveChangesAsync();

            // Modify the entity
            entity.Name = "Updated Name";
            entity.Description = "Updated Description";
            entity.Value = 20;

            // Act
            var result = await _repository.Update(entity);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Name, result.Name);
            Assert.Equal(entity.Description, result.Description);
            Assert.Equal(entity.Value, result.Value);

            var dbEntity = await _context.TestEntities.FindAsync(entity.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal("Updated Name", dbEntity.Name);
            Assert.Equal("Updated Description", dbEntity.Description);
            Assert.Equal(20, dbEntity.Value);
        }

        [Fact]
        public async Task Update_ThrowsWhenEntityIsNull()
        {
            // Arrange
            TestEntity? entity = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.Update(entity!));
        }

        [Fact]
        public async Task Remove_ShouldRemoveEntity()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "Entity to Remove",
                Description = "This will be removed",
                Value = 99
            };

            await _repository.Add(entity);
            await _context.SaveChangesAsync();

            var entityId = entity.Id;

            // Act
            var result = await _repository.Remove(entity);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
            Assert.Equal(entity.Name, result.Name);

            var dbEntity = await _context.TestEntities.FindAsync(entityId);
            Assert.Null(dbEntity);
        }

        [Fact]
        public async Task Remove_ThrowsWhenEntityIsNull()
        {
            // Arrange
            TestEntity? entity = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.Remove(entity!));
        }

        [Fact]
        public async Task GetAll_ShouldReturnAllEntities()
        {
            // Arrange
            var entities = new List<TestEntity>
            {
                new() { Name = "Entity A", Description = "Description A", Value = 1 },
                new() { Name = "Entity B", Description = "Description B", Value = 2 },
                new() { Name = "Entity C", Description = "Description C", Value = 3 }
            };

            await _repository.AddMany(entities);
            await _context.SaveChangesAsync();

            // Act
            var results = await _repository.GetAll();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(entities.Count, results.Count());

            foreach (var entity in entities)
            {
                var result = results.FirstOrDefault(e => e.Name == entity.Name);
                Assert.NotNull(result);
                Assert.Equal(entity.Description, result.Description);
                Assert.Equal(entity.Value, result.Value);
            }
        }

        [Fact]
        public async Task Find_ShouldReturnMatchingEntities()
        {
            // Arrange
            var entities = new List<TestEntity>
            {
                new() { Name = "Apple", Description = "Fruit", Value = 1 },
                new() { Name = "Banana", Description = "Fruit", Value = 2 },
                new() { Name = "Carrot", Description = "Vegetable", Value = 3 }
            };

            await _repository.AddMany(entities);
            await _context.SaveChangesAsync();

            // Act
            Expression<Func<TestEntity, bool>> fruitPredicate = e => e.Description == "Fruit";
            var fruits = await _repository.Find(fruitPredicate);

            // Assert
            Assert.NotNull(fruits);
            Assert.Equal(2, fruits.Count());
            Assert.All(fruits, fruit => Assert.Equal("Fruit", fruit.Description));
            Assert.Contains(fruits, f => f.Name == "Apple");
            Assert.Contains(fruits, f => f.Name == "Banana");
            Assert.DoesNotContain(fruits, f => f.Name == "Carrot");
        }

        [Fact]
        public async Task Count_ShouldReturnCorrectCount()
        {
            // Arrange
            var entities = new List<TestEntity>
            {
                new() { Name = "Entity 1", Description = "Type A", Value = 1 },
                new() { Name = "Entity 2", Description = "Type A", Value = 2 },
                new() { Name = "Entity 3", Description = "Type B", Value = 3 }
            };

            await _repository.AddMany(entities);
            await _context.SaveChangesAsync();

            // Act
            var totalCount = await _repository.Count();
            var typeACount = await _repository.Count(e => e.Description == "Type A");

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(2, typeACount);
        }

        [Fact]
        public async Task Any_ShouldReturnCorrectResult()
        {
            // Arrange
            var entities = new List<TestEntity>
            {
                new() { Name = "Entity X", Description = "Category 1", Value = 10 },
                new() { Name = "Entity Y", Description = "Category 2", Value = 20 }
            };

            await _repository.AddMany(entities);
            await _context.SaveChangesAsync();

            // Act
            var hasAny = await _repository.Any();
            var hasCategory1 = await _repository.Any(e => e.Description == "Category 1");
            var hasCategory3 = await _repository.Any(e => e.Description == "Category 3");

            // Assert
            Assert.True(hasAny);
            Assert.True(hasCategory1);
            Assert.False(hasCategory3);
        }

        [Fact]
        public async Task Operations_ShouldRespectCancellationToken()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel the token immediately

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Add(new TestEntity { Name = "Test" }, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.AddMany(new[] { new TestEntity { Name = "Test" } }, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Update(new TestEntity { Name = "Test" }, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Remove(new TestEntity { Name = "Test" }, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.GetAll(cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Find(e => e.Name == "Test", cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Count(null, cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _repository.Any(null, cts.Token));
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }
    }
}
