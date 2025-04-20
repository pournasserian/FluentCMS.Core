using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.Core.Interception.Abstractions;
using FluentCMS.Core.Interception.Extensions;
using FluentCMS.Core.Interception.Framework;
using FluentCMS.Core.Interception.Interceptors.HistoryTracking;
using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.Core.Repositories.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FluentCMS.Core.Interception.Tests;

public class EntityHistoryTests
{
    [Fact]
    public async Task EntityHistoryInterceptor_TracksEntityCreation()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock the history recorder
        var historyRecorderMock = new Mock<IHistoryRecorder>();
        
        // Register services
        services.AddSingleton(historyRecorderMock.Object);
        services.AddSingleton<IUserContextAccessor>(new DefaultUserContextAccessor("TestUser"));
        services.AddSingleton<IInterceptorChain, InterceptorChain>();
        services.AddSingleton<IMethodInterceptor, EntityHistoryInterceptor>();
        
        // Register a mock repository
        var repoMock = new Mock<IBaseEntityRepository<TestEntity>>();
        var testEntity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Description = "Test Description"
        };
        
        repoMock.Setup(r => r.Add(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testEntity);
            
        services.AddSingleton(repoMock.Object);
        
        // Create proxy with interception
        services.AddSingleton<IBaseEntityRepository<TestEntity>>(sp => {
            var implementation = sp.GetRequiredService<IBaseEntityRepository<TestEntity>>();
            var chain = sp.GetRequiredService<IInterceptorChain>();
            return ServiceProxy<IBaseEntityRepository<TestEntity>>.Create(implementation, chain);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetRequiredService<IBaseEntityRepository<TestEntity>>();
        
        // Act
        await repository.Add(testEntity);
        
        // Assert
        historyRecorderMock.Verify(
            r => r.RecordHistory(
                It.Is<TestEntity>(e => e.Id == testEntity.Id),
                "Create",
                "TestUser"),
            Times.Once);
    }
    
    [Fact]
    public async Task EntityHistoryInterceptor_TracksEntityUpdate()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock the history recorder
        var historyRecorderMock = new Mock<IHistoryRecorder>();
        
        // Register services
        services.AddSingleton(historyRecorderMock.Object);
        services.AddSingleton<IUserContextAccessor>(new DefaultUserContextAccessor("TestUser"));
        services.AddSingleton<IInterceptorChain, InterceptorChain>();
        services.AddSingleton<IMethodInterceptor, EntityHistoryInterceptor>();
        
        // Register a mock repository
        var repoMock = new Mock<IBaseEntityRepository<TestEntity>>();
        var testEntity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity Before Update",
            Description = "Test Description Before Update"
        };
        
        var updatedEntity = new TestEntity
        {
            Id = testEntity.Id,
            Name = "Test Entity After Update",
            Description = "Test Description After Update"
        };
        
        repoMock.Setup(r => r.GetById(testEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testEntity);
            
        repoMock.Setup(r => r.Update(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedEntity);
            
        services.AddSingleton(repoMock.Object);
        
        // Create proxy with interception
        services.AddSingleton<IBaseEntityRepository<TestEntity>>(sp => {
            var implementation = sp.GetRequiredService<IBaseEntityRepository<TestEntity>>();
            var chain = sp.GetRequiredService<IInterceptorChain>();
            return ServiceProxy<IBaseEntityRepository<TestEntity>>.Create(implementation, chain);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetRequiredService<IBaseEntityRepository<TestEntity>>();
        
        // Act
        await repository.Update(updatedEntity);
        
        // Assert
        historyRecorderMock.Verify(
            r => r.RecordHistory(
                It.Is<TestEntity>(e => e.Id == testEntity.Id && e.Name == "Test Entity Before Update"),
                "Update",
                "TestUser"),
            Times.Once);
    }
    
    [Fact]
    public async Task EntityHistoryInterceptor_TracksEntityDeletion()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock the history recorder
        var historyRecorderMock = new Mock<IHistoryRecorder>();
        
        // Register services
        services.AddSingleton(historyRecorderMock.Object);
        services.AddSingleton<IUserContextAccessor>(new DefaultUserContextAccessor("TestUser"));
        services.AddSingleton<IInterceptorChain, InterceptorChain>();
        services.AddSingleton<IMethodInterceptor, EntityHistoryInterceptor>();
        
        // Register a mock repository
        var repoMock = new Mock<IBaseEntityRepository<TestEntity>>();
        var testEntityId = Guid.NewGuid();
        var testEntity = new TestEntity
        {
            Id = testEntityId,
            Name = "Test Entity To Delete",
            Description = "Test Description To Delete"
        };
        
        repoMock.Setup(r => r.GetById(testEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testEntity);
            
        repoMock.Setup(r => r.Remove(testEntityId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        services.AddSingleton(repoMock.Object);
        
        // Create proxy with interception
        services.AddSingleton<IBaseEntityRepository<TestEntity>>(sp => {
            var implementation = sp.GetRequiredService<IBaseEntityRepository<TestEntity>>();
            var chain = sp.GetRequiredService<IInterceptorChain>();
            return ServiceProxy<IBaseEntityRepository<TestEntity>>.Create(implementation, chain);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetRequiredService<IBaseEntityRepository<TestEntity>>();
        
        // Act
        await repository.Remove(testEntityId);
        
        // Assert
        historyRecorderMock.Verify(
            r => r.RecordHistory(
                It.Is<TestEntity>(e => e.Id == testEntityId && e.Name == "Test Entity To Delete"),
                "Delete",
                "TestUser"),
            Times.Once);
    }
    
    [Fact]
    public void AddRepositoryWithHistoryTracking_RegistersAllComponents()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddRepositoryWithHistoryTracking<TestEntity, TestEntityRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var repo = serviceProvider.GetService<IBaseEntityRepository<TestEntity>>();
        var interceptor = serviceProvider.GetService<IMethodInterceptor>();
        var userContext = serviceProvider.GetService<IUserContextAccessor>();
        var chain = serviceProvider.GetService<IInterceptorChain>();
        
        Assert.NotNull(repo);
        Assert.NotNull(interceptor);
        Assert.NotNull(userContext);
        Assert.NotNull(chain);
        Assert.IsType<EntityHistoryInterceptor>(interceptor);
        Assert.IsType<DefaultUserContextAccessor>(userContext);
        Assert.IsType<InterceptorChain>(chain);
    }
    
    // Simple repository implementation for registration testing
    private class TestEntityRepository : IBaseEntityRepository<TestEntity>
    {
        public Task<TestEntity> Add(TestEntity entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);
            
        public Task<int> Count(Expression<Func<TestEntity, bool>>? filter = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
            
        public Task<IEnumerable<TestEntity>> GetAll(CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<TestEntity>());
            
        public Task<TestEntity> GetById(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TestEntity { Id = id });
            
        public Task<IEnumerable<TestEntity>> Query(
            Expression<Func<TestEntity, bool>>? filter = null,
            PaginationOptions? paginationOptions = null, 
            IList<SortOption<TestEntity>>? sortOptions = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<TestEntity>());
            
        public Task<IEnumerable<TestEntity>> Query(QueryOptions<TestEntity> options, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<TestEntity>());
            
        public Task Remove(Guid id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
            
        public Task<TestEntity> Update(TestEntity entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);
    }
}
