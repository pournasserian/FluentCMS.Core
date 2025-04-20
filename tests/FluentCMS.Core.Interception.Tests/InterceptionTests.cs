using FluentCMS.Core.Interception.Abstractions;
using FluentCMS.Core.Interception.Framework;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FluentCMS.Core.Interception.Tests;

public class InterceptionTests
{
    // Example service interface and implementation for testing
    public interface ITestService
    {
        string GetValue();
        int Add(int a, int b);
        Task<string> GetValueAsync();
    }

    public class TestService : ITestService
    {
        public string GetValue() => "Original Value";
        public int Add(int a, int b) => a + b;
        public Task<string> GetValueAsync() => Task.FromResult("Original Async Value");
    }

    [Fact]
    public void ServiceProxy_InvokesInterceptorMethods()
    {
        // Arrange
        var service = new TestService();
        var interceptorMock = new Mock<IMethodInterceptor>();
        interceptorMock.Setup(i => i.Order).Returns(0);
        
        var interceptors = new[] { interceptorMock.Object };
        var chain = new InterceptorChain(interceptors);
        var proxy = ServiceProxy<ITestService>.Create(service, chain);
        
        // Act
        var result = proxy.GetValue();
        
        // Assert
        interceptorMock.Verify(i => i.BeforeExecute(It.IsAny<MethodExecutionContext>()), Times.Once);
        interceptorMock.Verify(i => i.AfterExecute(It.IsAny<MethodExecutionContext>()), Times.Once);
        Assert.Equal("Original Value", result);
    }
    
    [Fact]
    public void ServiceProxy_HandlesExceptions()
    {
        // Arrange
        var serviceMock = new Mock<ITestService>();
        serviceMock.Setup(s => s.GetValue()).Throws(new InvalidOperationException("Test exception"));
        
        var interceptorMock = new Mock<IMethodInterceptor>();
        interceptorMock.Setup(i => i.Order).Returns(0);
        
        var interceptors = new[] { interceptorMock.Object };
        var chain = new InterceptorChain(interceptors);
        var proxy = ServiceProxy<ITestService>.Create(serviceMock.Object, chain);
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => proxy.GetValue());
        Assert.Equal("Test exception", exception.Message);
        
        interceptorMock.Verify(i => i.BeforeExecute(It.IsAny<MethodExecutionContext>()), Times.Once);
        interceptorMock.Verify(i => i.OnException(It.IsAny<MethodExecutionContext>()), Times.Once);
        interceptorMock.Verify(i => i.AfterExecute(It.IsAny<MethodExecutionContext>()), Times.Never);
    }
    
    [Fact]
    public void InterceptorChain_ExecutesInterceptorsInOrder()
    {
        // Arrange
        var service = new TestService();
        var callOrder = new List<string>();
        
        var interceptor1 = new TestInterceptor(1, callOrder, "Interceptor1");
        var interceptor2 = new TestInterceptor(2, callOrder, "Interceptor2");
        var interceptor3 = new TestInterceptor(3, callOrder, "Interceptor3");
        
        var interceptors = new IMethodInterceptor[] { interceptor3, interceptor1, interceptor2 };
        var chain = new InterceptorChain(interceptors);
        var proxy = ServiceProxy<ITestService>.Create(service, chain);
        
        // Act
        proxy.GetValue();
        
        // Assert
        Assert.Equal(6, callOrder.Count);
        Assert.Equal("Interceptor1_Before", callOrder[0]);
        Assert.Equal("Interceptor2_Before", callOrder[1]);
        Assert.Equal("Interceptor3_Before", callOrder[2]);
        Assert.Equal("Interceptor3_After", callOrder[3]);
        Assert.Equal("Interceptor2_After", callOrder[4]);
        Assert.Equal("Interceptor1_After", callOrder[5]);
    }
    
    [Fact]
    public void ServiceProxy_PassesCorrectArgumentsToMethod()
    {
        // Arrange
        var service = new TestService();
        var interceptorMock = new Mock<IMethodInterceptor>();
        interceptorMock.Setup(i => i.Order).Returns(0);
        
        var interceptors = new[] { interceptorMock.Object };
        var chain = new InterceptorChain(interceptors);
        var proxy = ServiceProxy<ITestService>.Create(service, chain);
        
        // Act
        var result = proxy.Add(5, 7);
        
        // Assert
        Assert.Equal(12, result);
        
        interceptorMock.Verify(i => i.BeforeExecute(It.Is<MethodExecutionContext>(
            ctx => ctx.Method.Name == "Add" && 
                  (int)ctx.Arguments[0] == 5 && 
                  (int)ctx.Arguments[1] == 7)), 
            Times.Once);
    }
    
    [Fact]
    public async Task ServiceProxy_WorksWithAsyncMethods()
    {
        // Arrange
        var service = new TestService();
        var interceptorMock = new Mock<IMethodInterceptor>();
        interceptorMock.Setup(i => i.Order).Returns(0);
        
        var interceptors = new[] { interceptorMock.Object };
        var chain = new InterceptorChain(interceptors);
        var proxy = ServiceProxy<ITestService>.Create(service, chain);
        
        // Act
        var result = await proxy.GetValueAsync();
        
        // Assert
        Assert.Equal("Original Async Value", result);
        interceptorMock.Verify(i => i.BeforeExecute(It.IsAny<MethodExecutionContext>()), Times.Once);
        interceptorMock.Verify(i => i.AfterExecute(It.IsAny<MethodExecutionContext>()), Times.Once);
    }
    
    [Fact]
    public void DependencyInjection_RegistersProxyCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Register the service with interception
        services.AddScoped<TestService>();
        services.AddScoped<IMethodInterceptor, TestInterceptor>();
        services.AddSingleton<IInterceptorChain, InterceptorChain>();
        services.AddScoped<ITestService>(sp => {
            var implementation = sp.GetRequiredService<TestService>();
            var chain = sp.GetRequiredService<IInterceptorChain>();
            return ServiceProxy<ITestService>.Create(implementation, chain);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var proxy = serviceProvider.GetRequiredService<ITestService>();
        var result = proxy.GetValue();
        
        // Assert
        Assert.Equal("Original Value", result);
    }
    
    private class TestInterceptor : IMethodInterceptor
    {
        private readonly int _order;
        private readonly List<string> _callOrder;
        private readonly string _name;
        
        public TestInterceptor(int order = 0, List<string>? callOrder = null, string name = "Test")
        {
            _order = order;
            _callOrder = callOrder ?? new List<string>();
            _name = name;
        }
        
        public int Order => _order;
        
        public void BeforeExecute(MethodExecutionContext context)
        {
            _callOrder.Add($"{_name}_Before");
        }
        
        public void AfterExecute(MethodExecutionContext context)
        {
            _callOrder.Add($"{_name}_After");
        }
        
        public void OnException(MethodExecutionContext context)
        {
            _callOrder.Add($"{_name}_Exception");
        }
    }
}
