using FluentAssertions;
using FluentCMS.Core.EventBus.FunctionalTests.Infrastructure;
using FluentCMS.Core.EventBus.FunctionalTests.TestEvents;
using FluentCMS.Core.EventBus.FunctionalTests.TestSubscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;

namespace FluentCMS.Core.EventBus.FunctionalTests.Tests;

public class ParallelPublishTests
{
    [Fact]
    public async Task Publish_ToMultipleDelayedSubscribers_ShouldExecuteInParallel()
    {
        // Arrange
        var testProvider = new TestServiceProvider();

        // Create multiple delayed subscribers with the same delay time
        var delayMs = 300;
        var subscriberCount = 3;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Register the necessary subscribers
        testProvider.RegisterService<TestEventSubscriber, TestEventSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventSubscriber>();

        for (int i = 0; i < subscriberCount; i++)
        {
            var logger = loggerFactory.CreateLogger<TestEventDelayedSubscriber>();
            var subscriber = new TestEventDelayedSubscriber(logger, delayMs);

            testProvider.RegisterInstance(subscriber);
            testProvider.RegisterEventSubscriber<TestEvent, TestEventDelayedSubscriber>();
        }

        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

        var testEvent = new TestEvent { Message = "Parallel Execution Test" };

        // Act - measure the time to complete the publish operation
        var stopwatch = Stopwatch.StartNew();
        await publisher.Publish(testEvent, "ParallelTest");
        stopwatch.Stop();

        // Assert
        // If execution is parallel, the time should be closer to a single subscriber's execution time
        // rather than the sum of all subscribers' execution times
        var executionTime = stopwatch.ElapsedMilliseconds;

        // We expect time to be closer to a single subscriber's delay than to (subscriberCount * delay)
        // We use a generous margin to avoid test flakiness due to system load variations
        executionTime.Should().BeLessThan((long)(delayMs * subscriberCount * 0.8),
            because: "parallel execution should complete in less time than sequential execution");

        // But it should take at least the time for one subscriber to complete
        executionTime.Should().BeGreaterThan((long)(delayMs * 0.8),
            because: "even with parallel execution, each subscriber still takes time to complete");

        // Cleanup
        testProvider.Dispose();
    }

    [Fact]
    public async Task Publish_ManyEventsToMultipleSubscribers_ShouldScaleEfficiently()
    {
        // Arrange
        var testProvider = new TestServiceProvider();

        // Register subscribers
        testProvider.RegisterService<TestEventSubscriber, TestEventSubscriber>();
        testProvider.RegisterService<TestEventCountingSubscriber, TestEventCountingSubscriber>();

        testProvider.RegisterEventSubscriber<TestEvent, TestEventSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventCountingSubscriber>();

        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var subscriber = serviceProvider.GetRequiredService<TestEventSubscriber>();
        var countingSubscriber = serviceProvider.GetRequiredService<TestEventCountingSubscriber>();

        // Create a batch of events to publish
        var eventCount = 100;
        var events = new List<TestEvent>();

        for (int i = 0; i < eventCount; i++)
        {
            events.Add(new TestEvent
            {
                Id = Guid.NewGuid(),
                Message = $"Batch Event {i}"
            });
        }

        // Act
        var stopwatch = Stopwatch.StartNew();

        // Publish all events - measure total time
        foreach (var ev in events)
        {
            await publisher.Publish(ev, "BatchTest");
        }

        stopwatch.Stop();

        // Assert
        // All events should be processed
        subscriber.GetReceivedEvents().Should().HaveCount(eventCount);
        countingSubscriber.GetEventCount().Should().Be(eventCount);

        // Log performance metrics
        var averageTimePerEvent = stopwatch.ElapsedMilliseconds / (double)eventCount;
        var eventsPerSecond = eventCount / (stopwatch.ElapsedMilliseconds / 1000.0);

        // These assertions ensure the implementation is reasonably efficient
        // but are not strict to avoid test failures on slower systems
        averageTimePerEvent.Should().BeLessThan(20,
            because: "event publishing should be efficient");
        eventsPerSecond.Should().BeGreaterThan(50,
            because: "the event bus should handle a reasonable throughput");

        // Cleanup
        testProvider.Dispose();
    }

    [Fact]
    public async Task Publish_MultipleEventsSerialized_ShouldMaintainCorrectOrder()
    {
        // Arrange
        var testProvider = new TestServiceProvider();

        testProvider.RegisterService<TestEventSubscriber, TestEventSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventSubscriber>();

        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var subscriber = serviceProvider.GetRequiredService<TestEventSubscriber>();

        // Create events with sequence numbers in their messages
        var eventCount = 25;
        var events = new List<TestEvent>();

        for (int i = 0; i < eventCount; i++)
        {
            events.Add(new TestEvent
            {
                Id = Guid.NewGuid(),
                Message = $"Sequence {i}"
            });
        }

        // Act - publish serially, one at a time
        foreach (var ev in events)
        {
            await publisher.Publish(ev, "SequenceTest");
        }

        // Assert - check that events were received in the same order they were sent
        var receivedEvents = subscriber.GetReceivedEvents();
        receivedEvents.Should().HaveCount(eventCount);

        for (int i = 0; i < eventCount; i++)
        {
            receivedEvents[i].Message.Should().Be($"Sequence {i}");
        }

        // Cleanup
        testProvider.Dispose();
    }
}
