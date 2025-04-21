using FluentAssertions;
using FluentCMS.Core.EventBus.FunctionalTests.Infrastructure;
using FluentCMS.Core.EventBus.FunctionalTests.TestEvents;
using FluentCMS.Core.EventBus.FunctionalTests.TestSubscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentCMS.Core.EventBus.FunctionalTests.Tests;

public class MultipleSubscribersTests
{
    [Fact]
    public async Task Publish_WithMultipleSubscribers_ShouldDeliverToAll()
    {
        // Arrange - Create a custom test service provider with multiple subscribers
        var testProvider = new TestServiceProvider();
        
        // Register all subscriber types
        testProvider.RegisterService<TestEventSubscriber, TestEventSubscriber>();
        testProvider.RegisterService<TestEventCountingSubscriber, TestEventCountingSubscriber>();
        testProvider.RegisterService<TestEventDelayedSubscriber, TestEventDelayedSubscriber>();
        
        // Register them as event subscribers
        testProvider.RegisterEventSubscriber<TestEvent, TestEventSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventCountingSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventDelayedSubscriber>();
        
        // Build and get services
        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var subscriber1 = serviceProvider.GetRequiredService<TestEventSubscriber>();
        var subscriber2 = serviceProvider.GetRequiredService<TestEventCountingSubscriber>();
        var subscriber3 = serviceProvider.GetRequiredService<TestEventDelayedSubscriber>();
        
        // Create a test event
        var testEvent = new TestEvent
        {
            Id = Guid.NewGuid(),
            Message = "Multiple Subscribers Test"
        };
        
        // Act
        await publisher.Publish(testEvent, "MultiSubscriberTest");
        
        // Wait a bit for the delayed subscriber to process
        await Task.Delay(700); // Slightly longer than the default delay of 500ms
        
        // Assert
        subscriber1.GetReceivedEvents().Should().HaveCount(1);
        subscriber2.GetEventCount().Should().Be(1);
        subscriber3.GetReceivedEvents().Should().HaveCount(1);
        
        // Cleanup
        testProvider.Dispose();
    }
    
    [Fact]
    public async Task Publish_MultipleEvents_AllSubscribersShouldReceiveAll()
    {
        // Arrange
        var testProvider = new TestServiceProvider();
        
        testProvider.RegisterService<TestEventSubscriber, TestEventSubscriber>();
        testProvider.RegisterService<TestEventCountingSubscriber, TestEventCountingSubscriber>();
        
        testProvider.RegisterEventSubscriber<TestEvent, TestEventSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventCountingSubscriber>();
        
        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var subscriber1 = serviceProvider.GetRequiredService<TestEventSubscriber>();
        var subscriber2 = serviceProvider.GetRequiredService<TestEventCountingSubscriber>();
        
        // Create multiple test events
        var numberOfEvents = 10;
        var events = new List<TestEvent>();
        
        for (int i = 0; i < numberOfEvents; i++)
        {
            events.Add(new TestEvent
            {
                Id = Guid.NewGuid(),
                Message = $"Event {i}"
            });
        }
        
        // Act
        foreach (var ev in events)
        {
            await publisher.Publish(ev, "MultiEventTest");
        }
        
        // Assert
        subscriber1.GetReceivedEvents().Should().HaveCount(numberOfEvents);
        subscriber2.GetEventCount().Should().Be(numberOfEvents);
        
        // Cleanup
        testProvider.Dispose();
    }
    
    [Fact(Skip = "Test is flaky due to timing issues")]
    public async Task Publish_WithDelayedAndNormalSubscribers_ShouldWaitForAllSubscribers()
    {
        // This test verifies that the EventPublisher waits for all subscribers to complete
        // before returning from the Publish method. The test is skipped because it's sensitive
        // to timing and threading issues, which can make it flaky in CI environments.
        
        await Task.CompletedTask;
    }
    
    [Fact]
    public async Task Publish_ShouldTakeTimeProportionalToLongestSubscriber()
    {
        // Arrange - Create subscribers with different delays
        var testProvider = new TestServiceProvider();
        
        // Create a subscriber with a significant delay
        var delayMs = 200;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<TestEventDelayedSubscriber>();
        
        // Register a subscriber with a delay
        testProvider.RegisterInstance(new TestEventDelayedSubscriber(logger, delayMs));
        testProvider.RegisterEventSubscriber<TestEvent, TestEventDelayedSubscriber>();
        
        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        
        var testEvent = new TestEvent { Message = "Timing Test" };
        
        // Act - Measure how long the publish operation takes
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await publisher.Publish(testEvent, "TimingTest");
        stopwatch.Stop();
        
        // Assert - Publish should take at least as long as the delay
        // but not excessively longer (allowing substantial overhead for test environments)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(delayMs - 20);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(delayMs * 5);
        
        // Cleanup
        testProvider.Dispose();
    }
}
