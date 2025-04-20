using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Core.Interception.Tests;

public class AdvancedInterceptionTests
{
    // Test interface and implementation for advanced scenarios
    public interface IAdvancedService
    {
        string GetData();
        int Calculate(int a, int b);
        Task<string> GetDataAsync();
        void LogMessage(string message);
        [NoIntercept]
        string GetUninterceptedData();
    }

    // Custom attribute to mark methods that should not be intercepted
    [AttributeUsage(AttributeTargets.Method)]
    public class NoInterceptAttribute : Attribute { }

    public class AdvancedService : IAdvancedService
    {
        public string GetData() => "Original Data";
        public int Calculate(int a, int b) => a + b;
        public Task<string> GetDataAsync() => Task.FromResult("Original Async Data");
        public void LogMessage(string message) { /* Just a test method */ }
        public string GetUninterceptedData() => "Unintercepted Data";
    }

    // Ordered interceptors for testing multiple interceptor scenarios
    public class FirstInterceptor : AspectInterceptorBase
    {
        public override int Order => 1;
        public List<string> CallLog { get; } = new();

        public override void OnBefore(MethodInfo? method, object?[]? arguments, object instance)
        {
            CallLog.Add($"First:Before:{method?.Name}");
        }

        public override void OnAfter(MethodInfo? method, object?[]? arguments, object instance, object? returnValue)
        {
            CallLog.Add($"First:After:{method?.Name}");
        }

        public override object? OnReturn(MethodInfo? method, object?[]? arguments, object instance, object? returnValue)
        {
            CallLog.Add($"First:Return:{method?.Name}");
            
            // Modify string return values
            if (returnValue is string strValue)
                return $"First:{strValue}";
            
            return returnValue;
        }
    }

    public class SecondInterceptor : AspectInterceptorBase
    {
        public override int Order => 2;
        public List<string> CallLog { get; } = new();

        public override void OnBefore(MethodInfo? method, object?[]? arguments, object instance)
        {
            CallLog.Add($"Second:Before:{method?.Name}");
        }

        public override void OnAfter(MethodInfo? method, object?[]? arguments, object instance, object? returnValue)
        {
            CallLog.Add($"Second:After:{method?.Name}");
        }

        public override object? OnReturn(MethodInfo? method, object?[]? arguments, object instance, object? returnValue)
        {
            CallLog.Add($"Second:Return:{method?.Name}");
            
            // Modify string return values
            if (returnValue is string strValue)
                return $"Second:{strValue}";
            
            return returnValue;
        }
    }

    // Interceptor that counts method calls
    public class CountingInterceptor : AspectInterceptorBase
    {
        public Dictionary<string, int> MethodCalls { get; } = new();

        public override void OnBefore(MethodInfo? method, object?[]? arguments, object instance)
        {
            if (method?.Name != null)
            {
                if (!MethodCalls.ContainsKey(method.Name))
                    MethodCalls[method.Name] = 0;
                
                MethodCalls[method.Name]++;
            }
        }
    }

    [Fact]
    public void Multiple_Interceptors_Should_Execute_In_Order()
    {
        // Arrange
        var services = new ServiceCollection();
        var first = new FirstInterceptor();
        var second = new SecondInterceptor();

        services.AddInterceptedTransient<IAdvancedService, AdvancedService>()
            .AddInterceptor(first)
            .AddInterceptor(second)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IAdvancedService>();

        // Act
        var result = service.GetData();

        // Assert
        result.Should().Be("Second:First:Original Data");
        
        // Verify execution order
        first.CallLog.Should().HaveCount(3);
        second.CallLog.Should().HaveCount(3);
        
        // Before methods should be called in ascending order (1 then 2)
        first.CallLog[0].Should().Be("First:Before:GetData");
        second.CallLog[0].Should().Be("Second:Before:GetData");
        
        // Return methods should be called in ascending order (1 then 2)
        first.CallLog[1].Should().Be("First:Return:GetData");
        second.CallLog[1].Should().Be("Second:Return:GetData");
        
        // After methods should be called in ascending order (1 then 2)
        first.CallLog[2].Should().Be("First:After:GetData");
        second.CallLog[2].Should().Be("Second:After:GetData");
    }

    [Fact]
    public void Method_Filtering_Should_Apply_Interceptors_Selectively()
    {
        // Arrange
        var services = new ServiceCollection();
        var counter = new CountingInterceptor();

        // Only intercept methods that return strings
        services.AddInterceptedTransient<IAdvancedService, AdvancedService>()
            .AddInterceptor(counter, method => method.ReturnType == typeof(string))
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IAdvancedService>();

        // Act
        service.GetData();             // Should be intercepted (returns string)
        service.Calculate(1, 2);       // Should NOT be intercepted (returns int)
        service.LogMessage("test");    // Should NOT be intercepted (returns void)
        service.GetUninterceptedData(); // Should be intercepted (returns string)

        // Assert
        counter.MethodCalls.Should().ContainKey("GetData");
        counter.MethodCalls.Should().ContainKey("GetUninterceptedData");
        counter.MethodCalls.Should().NotContainKey("Calculate");
        counter.MethodCalls.Should().NotContainKey("LogMessage");
    }

    [Fact(Skip = "Need to investigate attribute behavior in the framework")]
    public void Interceptor_Groups_Should_Respect_Method_Filters()
    {
        // Arrange
        var services = new ServiceCollection();
        var counter = new CountingInterceptor();

        // Create a group that only applies to methods with the NoIntercept attribute
        // Since we put the NoIntercept attribute on methods we want intercepted (for testing purposes)
        // This filter is specifically looking for methods with this attribute
        services.AddInterceptedTransient<IAdvancedService, AdvancedService>()
            .AddInterceptor(counter, method => method.GetCustomAttribute<NoInterceptAttribute>() != null)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IAdvancedService>();

        // Act - Call both methods
        service.GetData();              // Should NOT be intercepted (no attribute)
        service.GetUninterceptedData(); // Should be intercepted (has attribute)

        // Assert
        counter.MethodCalls.Should().NotContainKey("GetData");
        counter.MethodCalls.Should().ContainKey("GetUninterceptedData");
    }

    [Fact]
    public void Multiple_Interceptor_Groups_Should_Work_Together()
    {
        // Arrange
        var services = new ServiceCollection();
        var stringMethodsCounter = new CountingInterceptor();
        var voidMethodsCounter = new CountingInterceptor();

        services.AddInterceptedTransient<IAdvancedService, AdvancedService>()
            // Group 1: Intercept string methods
            .CreateInterceptorGroup()
                .AddInterceptor(stringMethodsCounter)
                .WithMethodFilter(method => method.ReturnType == typeof(string))
            .EndGroup()
            // Group 2: Intercept void methods
            .CreateInterceptorGroup()
                .AddInterceptor(voidMethodsCounter)
                .WithMethodFilter(method => method.ReturnType == typeof(void))
            .EndGroup()
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IAdvancedService>();

        // Act
        service.GetData();          // Should be intercepted by Group 1
        service.Calculate(1, 2);    // Should NOT be intercepted by either group
        service.LogMessage("test"); // Should be intercepted by Group 2

        // Assert
        stringMethodsCounter.MethodCalls.Should().ContainKey("GetData");
        stringMethodsCounter.MethodCalls.Should().NotContainKey("Calculate");
        stringMethodsCounter.MethodCalls.Should().NotContainKey("LogMessage");

        voidMethodsCounter.MethodCalls.Should().NotContainKey("GetData");
        voidMethodsCounter.MethodCalls.Should().NotContainKey("Calculate");
        voidMethodsCounter.MethodCalls.Should().ContainKey("LogMessage");
    }
}
