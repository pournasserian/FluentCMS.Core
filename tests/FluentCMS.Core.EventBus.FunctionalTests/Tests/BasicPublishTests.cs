namespace FluentCMS.Core.EventBus.FunctionalTests.Tests;

public class BasicPublishTests : IClassFixture<EventBusTestFixture>
{
    private readonly EventBusTestFixture _fixture;
    private readonly IEventPublisher _publisher;
    private readonly TestEventSubscriber _subscriber;
    private readonly TestEventCountingSubscriber _countingSubscriber;

    public BasicPublishTests(EventBusTestFixture fixture)
    {
        _fixture = fixture;
        _publisher = fixture.EventPublisher;
        _subscriber = fixture.GetSubscriber<TestEventSubscriber>();
        _countingSubscriber = fixture.GetSubscriber<TestEventCountingSubscriber>();

        // Clear any previously received events
        _subscriber.ClearEvents();
        _countingSubscriber.Reset();
    }

    [Fact]
    public async Task Publish_SingleEvent_ShouldBeReceivedBySubscriber()
    {
        // Arrange
        var testEvent = _fixture.CreateTestEvent("Test Message");

        // Act
        await _publisher.Publish(testEvent, "TestEvent");

        // Assert
        _subscriber.GetReceivedEvents().Should().HaveCount(1);
        _subscriber.GetReceivedEvents().First().Id.Should().Be(testEvent.Id);
        _subscriber.GetReceivedEvents().First().Message.Should().Be("Test Message");
    }

    [Fact]
    public async Task Publish_MultipleEvents_ShouldBeReceivedInOrder()
    {
        // Arrange
        var testEvent1 = _fixture.CreateTestEvent("Message 1");
        var testEvent2 = _fixture.CreateTestEvent("Message 2");
        var testEvent3 = _fixture.CreateTestEvent("Message 3");

        // Act
        await _publisher.Publish(testEvent1, "TestEvent");
        await _publisher.Publish(testEvent2, "TestEvent");
        await _publisher.Publish(testEvent3, "TestEvent");

        // Assert
        _subscriber.GetReceivedEvents().Should().HaveCount(3);
        _subscriber.GetReceivedEvents()[0].Message.Should().Be("Message 1");
        _subscriber.GetReceivedEvents()[1].Message.Should().Be("Message 2");
        _subscriber.GetReceivedEvents()[2].Message.Should().Be("Message 3");
    }

    [Fact]
    public async Task Publish_SingleEvent_ShouldBeReceivedByMultipleSubscribers()
    {
        // Arrange
        var testEvent = _fixture.CreateTestEvent();

        // Act
        await _publisher.Publish(testEvent, "TestEvent");

        // Assert
        _subscriber.GetReceivedEvents().Should().HaveCount(1);
        _countingSubscriber.GetEventCount().Should().Be(1);
    }

    [Fact]
    public async Task Publish_MultipleEvents_ShouldUpdateCountCorrectly()
    {
        // Arrange
        var eventCount = 5;

        // Act
        for (int i = 0; i < eventCount; i++)
        {
            await _publisher.Publish(_fixture.CreateTestEvent($"Message {i}"), "TestEvent");
        }

        // Assert
        _countingSubscriber.GetEventCount().Should().Be(eventCount);
    }

    [Fact]
    public async Task Publish_UsingDomainEvent_ShouldBeReceivedBySubscriber()
    {
        // Arrange
        var testEvent = _fixture.CreateTestEvent("Domain Event Test");
        var domainEvent = new DomainEvent<TestEvent>(testEvent, "DomainTestEvent");

        // Act
        await _publisher.Publish(domainEvent);

        // Assert
        _subscriber.GetReceivedEvents().Should().HaveCount(1);
        _subscriber.GetReceivedEvents().First().Message.Should().Be("Domain Event Test");
    }
}
