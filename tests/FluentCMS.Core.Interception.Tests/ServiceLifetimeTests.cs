using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FluentCMS.Core.Interception.Tests;

public class ServiceLifetimeTests
{
    // Simple service for testing different lifetimes
    public interface ILifetimeService
    {
        Guid GetId();
        int GetCount();
        void Increment();
    }

    public class LifetimeService : ILifetimeService
    {
        private readonly Guid _id = Guid.NewGuid();
        private int _count = 0;

        public Guid GetId() => _id;
        public int GetCount() => _count;
        public void Increment() => _count++;
    }

    // Simple interceptor that tracks method calls
    public class LifetimeInterceptor : AspectInterceptorBase
    {
        private readonly Guid _id = Guid.NewGuid();
        private int _callCount = 0;

        public Guid Id => _id;
        public int CallCount => _callCount;

        public override void OnBefore(MethodInfo? method, object?[]? arguments, object instance)
        {
            _callCount++;
        }
    }

    [Fact]
    public void Transient_Services_Should_Create_New_Instances()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new LifetimeInterceptor();

        services.AddInterceptedTransient<ILifetimeService, LifetimeService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service1 = serviceProvider.GetRequiredService<ILifetimeService>();
        var service2 = serviceProvider.GetRequiredService<ILifetimeService>();

        // Assert
        service1.Should().NotBeSameAs(service2);
        service1.GetId().Should().NotBe(service2.GetId());
    }

    [Fact]
    public void Scoped_Services_Should_Share_Instances_Within_Scope()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new LifetimeInterceptor();

        services.AddInterceptedScoped<ILifetimeService, LifetimeService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - First scope
        using (var scope1 = serviceProvider.CreateScope())
        {
            var service1A = scope1.ServiceProvider.GetRequiredService<ILifetimeService>();
            var service1B = scope1.ServiceProvider.GetRequiredService<ILifetimeService>();

            // Services within the same scope should be the same instance
            service1A.Should().BeSameAs(service1B);
            service1A.GetId().Should().Be(service1B.GetId());

            // Test state is maintained
            service1A.Increment();
            service1B.GetCount().Should().Be(1);
        }

        // Act & Assert - Second scope
        using (var scope2 = serviceProvider.CreateScope())
        {
            var service2 = scope2.ServiceProvider.GetRequiredService<ILifetimeService>();

            // Service from a different scope should be a different instance
            service2.GetCount().Should().Be(0);
        }
    }

    [Fact]
    public void Singleton_Services_Should_Share_Instance_Across_Provider()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new LifetimeInterceptor();

        services.AddInterceptedSingleton<ILifetimeService, LifetimeService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service1 = serviceProvider.GetRequiredService<ILifetimeService>();
        var service2 = serviceProvider.GetRequiredService<ILifetimeService>();

        service1.Increment();

        // Get from a different scope
        using (var scope = serviceProvider.CreateScope())
        {
            var service3 = scope.ServiceProvider.GetRequiredService<ILifetimeService>();

            // Assert
            service1.Should().BeSameAs(service2);
            service1.GetId().Should().Be(service2.GetId());

            // Should be the same instance even in a different scope
            service1.Should().BeSameAs(service3);
            service3.GetCount().Should().Be(1); // Should maintain the incremented count
        }
    }

    [Fact]
    public void Interceptors_Should_Be_Resolved_From_ServiceProvider_When_Registered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register the interceptor in the service collection
        services.AddSingleton<LifetimeInterceptor>();

        // Create and use the registered interceptor
        var interceptor = new LifetimeInterceptor();
        services.AddSingleton(interceptor);

        services.AddInterceptedTransient<ILifetimeService, LifetimeService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service = serviceProvider.GetRequiredService<ILifetimeService>();
        service.GetId(); // Call a method to trigger the interceptor

        // Also get the interceptor directly from the service provider
        var resolvedInterceptor = serviceProvider.GetRequiredService<LifetimeInterceptor>();

        // Assert
        interceptor.CallCount.Should().Be(1);
    }

    [Fact]
    public void Proxy_Should_Forward_To_Implementation_Method()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new LifetimeInterceptor();

        services.AddInterceptedTransient<ILifetimeService, LifetimeService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ILifetimeService>();

        // Act
        var initialCount = service.GetCount();
        service.Increment();
        var newCount = service.GetCount();

        // Assert
        initialCount.Should().Be(0);
        newCount.Should().Be(1);
    }

    // This class combines all lifetime tests
    [Fact]
    public void Combined_Lifetime_Test()
    {
        // Arrange
        var services = new ServiceCollection();

        // Create and register the interceptor
        var interceptor = new LifetimeInterceptor();
        services.AddSingleton(interceptor);

        services.AddInterceptedTransient<ILifetimeService, LifetimeService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var transient1 = serviceProvider.GetRequiredService<ILifetimeService>();
        var transient2 = serviceProvider.GetRequiredService<ILifetimeService>();

        // Call methods to trigger the interceptor
        transient1.GetId();
        transient2.GetId();

        // Assert
        transient1.Should().NotBeSameAs(transient2);
        interceptor.CallCount.Should().Be(2); // Both calls to GetId should increment the counter
    }
}
