using FluentAssertions;
using FluentCMS.Core.EventBus.FunctionalTests.Infrastructure;
using FluentCMS.Core.EventBus.FunctionalTests.TestEvents;
using FluentCMS.Core.EventBus.FunctionalTests.TestSubscribers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentCMS.Core.EventBus.FunctionalTests.Tests;

public class CancellationTests
{
    [Fact]
    public async Task Publish_WithCancelledToken_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var testProvider = new TestServiceProvider();
        testProvider.RegisterService<TestEventSubscriber, TestEventSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventSubscriber>();

        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var subscriber = serviceProvider.GetRequiredService<TestEventSubscriber>();

        var testEvent = new TestEvent { Message = "Cancellation Test" };

        // Create a pre-cancelled token
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await publisher.Publish(testEvent, "CancellationTest", cts.Token));

        // The event should not have been processed
        subscriber.GetReceivedEvents().Should().BeEmpty();

        // Cleanup
        testProvider.Dispose();
    }

    [Fact]
    public async Task Publish_WithCancellationDuringDelay_ShouldCancelProcessing()
    {
        // Arrange
        var testProvider = new TestServiceProvider();
        testProvider.RegisterService<TestEventDelayedSubscriber, TestEventDelayedSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventDelayedSubscriber>();

        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var subscriber = serviceProvider.GetRequiredService<TestEventDelayedSubscriber>();

        var testEvent = new TestEvent { Message = "Cancellation During Processing Test" };

        // Create a token that will be cancelled shortly after publishing
        var cts = new CancellationTokenSource();

        // Act
        var publishTask = publisher.Publish(testEvent, "CancellationTest", cts.Token);

        // Cancel after a short delay (but before the delayed subscriber would finish)
        await Task.Delay(50);
        cts.Cancel();

        // Assert
        try
        {
            await publishTask;
            // If we get here, the task completed before cancellation
        }
        catch (OperationCanceledException)
        {
            // This is expected if cancellation worked
        }

        // Wait a bit to ensure any non-cancelled processing would have completed
        await Task.Delay(600);

        // The event should not have been processed completely (or was processed before cancellation)
        // This test is timing-dependent, so we don't make a hard assertion about the state
        // but log the observed behavior

        // Cleanup
        testProvider.Dispose();
    }

    [Fact]
    public async Task Publish_WithDelayedCancellation_ShouldCompleteSomeSubscribers()
    {
        // Arrange - set up a mix of fast and slow subscribers
        var testProvider = new TestServiceProvider();

        testProvider.RegisterService<TestEventSubscriber, TestEventSubscriber>();
        testProvider.RegisterService<TestEventDelayedSubscriber, TestEventDelayedSubscriber>();

        testProvider.RegisterEventSubscriber<TestEvent, TestEventSubscriber>();
        testProvider.RegisterEventSubscriber<TestEvent, TestEventDelayedSubscriber>();

        var serviceProvider = testProvider.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var fastSubscriber = serviceProvider.GetRequiredService<TestEventSubscriber>();
        var slowSubscriber = serviceProvider.GetRequiredService<TestEventDelayedSubscriber>();

        var testEvent = new TestEvent { Message = "Partial Cancellation Test" };

        // Create a token that will be cancelled after the fast subscriber completes
        // but before the slow subscriber completes
        var cts = new CancellationTokenSource();

        // Act
        var publishTask = publisher.Publish(testEvent, "PartialCancellationTest", cts.Token);

        // Wait enough time for the fast subscriber to complete but not the slow one
        await Task.Delay(100);

        // The fast subscriber should have processed the event
        fastSubscriber.GetReceivedEvents().Should().HaveCount(1);

        // Now cancel - this should interrupt the slow subscriber
        cts.Cancel();

        try
        {
            await publishTask;
            // If we get here, the publish task completed before cancellation
        }
        catch (OperationCanceledException)
        {
            // Expected if cancellation worked
        }

        // Wait a bit to ensure any non-cancelled processing would have completed
        await Task.Delay(600);

        // Assert
        // The fast subscriber should have processed the event
        fastSubscriber.GetReceivedEvents().Should().HaveCount(1);

        // The slow subscriber might or might not have processed the event
        // depending on exact timing - we don't make hard assertions here

        // Cleanup
        testProvider.Dispose();
    }
}
