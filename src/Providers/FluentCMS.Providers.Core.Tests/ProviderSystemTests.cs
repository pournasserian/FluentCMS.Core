using FluentCMS.Providers.Caching.Abstractions;
using FluentCMS.Providers.Core.Abstractions;
using FluentCMS.Providers.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FluentCMS.Providers.Core.Tests;

public class ProviderSystemTests
{
    private readonly IServiceProvider _serviceProvider;

    public ProviderSystemTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Add the provider system
        services.AddProviderSystem(configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Should_Register_Active_Cache_Provider()
    {
        // Act
        var cacheProvider = _serviceProvider.GetService<ICacheProvider>();

        // Assert
        Assert.NotNull(cacheProvider);
        Assert.IsType<FluentCMS.Providers.Caching.InMemory.InMemoryCacheProvider>(cacheProvider);
    }

    [Fact]
    public void Should_Register_Cache_Provider_Factory()
    {
        // Act
        var factory = _serviceProvider.GetService<IProviderFactory<ICacheProvider>>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Factory_Should_Return_Active_Provider()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IProviderFactory<ICacheProvider>>();

        // Act
        var activeProvider = factory.GetActiveProvider();

        // Assert
        Assert.NotNull(activeProvider);
        Assert.IsType<FluentCMS.Providers.Caching.InMemory.InMemoryCacheProvider>(activeProvider);
    }

    [Fact]
    public void Factory_Should_Return_Named_Provider()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IProviderFactory<ICacheProvider>>();

        // Act
        var mainCache = factory.GetProvider("MainCache");
        var fastCache = factory.GetProvider("FastCache");

        // Assert
        Assert.NotNull(mainCache);
        Assert.NotNull(fastCache);
        Assert.IsType<FluentCMS.Providers.Caching.InMemory.InMemoryCacheProvider>(mainCache);
        Assert.IsType<FluentCMS.Providers.Caching.InMemory.InMemoryCacheProvider>(fastCache);

        // They should be different instances
        Assert.NotSame(mainCache, fastCache);
    }

    [Fact]
    public void Factory_Should_Check_Provider_Existence()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IProviderFactory<ICacheProvider>>();

        // Act & Assert
        Assert.True(factory.HasProvider("MainCache"));
        Assert.True(factory.HasProvider("FastCache"));
        Assert.False(factory.HasProvider("NonExistentCache"));
    }

    [Fact]
    public void Factory_Should_Return_All_Providers()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IProviderFactory<ICacheProvider>>();

        // Act
        var allProviders = factory.GetAllProviders();

        // Assert
        Assert.NotNull(allProviders);
        Assert.Equal(2, allProviders.Count);
        Assert.True(allProviders.ContainsKey("MainCache"));
        Assert.True(allProviders.ContainsKey("FastCache"));
    }

    [Fact]
    public async Task Cache_Provider_Should_Work_Correctly()
    {
        // Arrange
        var cacheProvider = _serviceProvider.GetRequiredService<ICacheProvider>();
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await cacheProvider.Set(key, value, TimeSpan.FromMinutes(5));
        var retrievedValue = await cacheProvider.Get<string>(key);

        // Assert
        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    public async Task Named_Cache_Providers_Should_Be_Independent()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IProviderFactory<ICacheProvider>>();
        var mainCache = factory.GetProvider("MainCache");
        var fastCache = factory.GetProvider("FastCache");

        const string key = "independence-test";
        const string mainValue = "main-cache-value";
        const string fastValue = "fast-cache-value";

        // Act
        await mainCache.Set(key, mainValue, TimeSpan.FromMinutes(5));
        await fastCache.Set(key, fastValue, TimeSpan.FromMinutes(5));

        var mainRetrieved = await mainCache.Get<string>(key);
        var fastRetrieved = await fastCache.Get<string>(key);

        // Assert
        Assert.Equal(mainValue, mainRetrieved);
        Assert.Equal(fastValue, fastRetrieved);
        Assert.NotEqual(mainRetrieved, fastRetrieved);
    }

    [Fact]
    public void Should_Throw_When_Getting_Non_Existent_Provider()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IProviderFactory<ICacheProvider>>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.GetProvider("NonExistentProvider"));
    }
}
