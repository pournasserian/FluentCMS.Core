namespace FluentCMS.Core.Interception.Tests;

public class BasicInterceptionTests
{
    // Test interfaces and classes
    public interface ITestService
    {
        string GetData();
        int Add(int a, int b);
        void DoSomething();
        Task<string> GetDataAsync();
        Task DoSomethingAsync();
    }

    public class TestService : ITestService
    {
        public string GetData() => "Original Data";
        public int Add(int a, int b) => a + b;
        public void DoSomething() { /* Do nothing */ }
        public Task<string> GetDataAsync() => Task.FromResult("Original Async Data");
        public Task DoSomethingAsync() => Task.CompletedTask;
    }

    // Custom interceptor for testing
    public class TestInterceptor : AspectInterceptorBase
    {
        public bool BeforeCalled { get; private set; }
        public bool AfterCalled { get; private set; }
        public bool ExceptionCalled { get; private set; }
        public bool ReturnCalled { get; private set; }
        public object? LastReturnValue { get; private set; }
        public string? LastMethodName { get; private set; }
        public object?[]? LastArguments { get; private set; }
        public Exception? LastException { get; private set; }

        public override void OnBefore(MethodInfo? method, object?[]? arguments, object instance)
        {
            BeforeCalled = true;
            LastMethodName = method?.Name;
            LastArguments = arguments;
        }

        public override void OnAfter(MethodInfo? method, object?[]? arguments, object instance, object? returnValue)
        {
            AfterCalled = true;
            LastReturnValue = returnValue;
        }

        public override void OnException(MethodInfo? method, object?[]? arguments, object instance, Exception? exception)
        {
            ExceptionCalled = true;
            LastException = exception;
        }

        public override object? OnReturn(MethodInfo? method, object?[]? arguments, object instance, object? returnValue)
        {
            ReturnCalled = true;
            LastReturnValue = returnValue;

            // For testing return value modification
            if (method?.Name == "GetData")
                return "Modified Data";

            if (method?.Name == "GetDataAsync" && returnValue is Task<string> task)
                return Task.FromResult("Modified Async Data");

            return returnValue;
        }

        public void Reset()
        {
            BeforeCalled = false;
            AfterCalled = false;
            ExceptionCalled = false;
            ReturnCalled = false;
            LastReturnValue = null;
            LastMethodName = null;
            LastArguments = null;
            LastException = null;
        }
    }

    [Fact]
    public void Interceptor_Should_Be_Called_For_Sync_Methods()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new TestInterceptor();

        services.AddInterceptedTransient<ITestService, TestService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ITestService>();

        // Act
        var result = service.GetData();

        // Assert
        interceptor.BeforeCalled.Should().BeTrue();
        interceptor.AfterCalled.Should().BeTrue();
        interceptor.ReturnCalled.Should().BeTrue();
        interceptor.LastMethodName.Should().Be("GetData");
        result.Should().Be("Modified Data");
    }

    [Fact]
    public void Interceptor_Should_Handle_Method_Arguments()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new TestInterceptor();

        services.AddInterceptedTransient<ITestService, TestService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ITestService>();

        // Act
        var result = service.Add(5, 10);

        // Assert
        interceptor.BeforeCalled.Should().BeTrue();
        interceptor.LastArguments.Should().NotBeNull();
        interceptor.LastArguments.Should().HaveCount(2);
        interceptor.LastArguments![0].Should().Be(5);
        interceptor.LastArguments![1].Should().Be(10);
        result.Should().Be(15);
    }

    [Fact]
    public void Interceptor_Should_Handle_Void_Methods()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new TestInterceptor();

        services.AddInterceptedTransient<ITestService, TestService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ITestService>();

        // Act
        service.DoSomething();

        // Assert
        interceptor.BeforeCalled.Should().BeTrue();
        interceptor.AfterCalled.Should().BeTrue();
        interceptor.ReturnCalled.Should().BeTrue();
        interceptor.LastMethodName.Should().Be("DoSomething");
    }

    [Fact]
    public void Interceptor_Should_Handle_Exceptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var interceptor = new TestInterceptor();
        var expectedMessage = "Test exception";

        services.AddInterceptedTransient<ITestService, MockThrowingService>()
            .AddInterceptor(interceptor)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ITestService>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.GetData());
        exception.Message.Should().Be(expectedMessage);

        interceptor.BeforeCalled.Should().BeTrue();
        interceptor.ExceptionCalled.Should().BeTrue();
        interceptor.AfterCalled.Should().BeFalse();
        interceptor.LastException.Should().BeOfType<InvalidOperationException>();
        interceptor.LastException!.Message.Should().Be(expectedMessage);
    }

    // This service throws exceptions for testing exception handling
    private class MockThrowingService : ITestService
    {
        public string GetData() => throw new InvalidOperationException("Test exception");
        public int Add(int a, int b) => throw new InvalidOperationException("Test exception");
        public void DoSomething() => throw new InvalidOperationException("Test exception");
        public Task<string> GetDataAsync() => throw new InvalidOperationException("Test exception");
        public Task DoSomethingAsync() => throw new InvalidOperationException("Test exception");
    }
}
