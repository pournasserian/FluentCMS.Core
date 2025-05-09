﻿using FluentCMS.Core.EventBus.Abstractions;

namespace FluentCMS.DataAccess.Abstractions;

public interface IRepositortyEventPublisher
{
    Task PublishCreated(RepositoryEntityCreatedEvent repsitoryEvent, CancellationToken cancellationToken = default);
    Task PublishUpdated(RepositoryEntityUpdatedEvent repsitoryEvent, CancellationToken cancellationToken = default);
    Task PublishRemoved(RepositoryEntityRemovedEvent repsitoryEvent, CancellationToken cancellationToken = default);
}

public class RepositoryEvent(object data, string eventType) : IEvent
{
    public object Data { get => data; }
    public string EventType { get => eventType; }
}

public class RepositoryEntityCreatedEvent(object data) : RepositoryEvent(data, $"{data.GetType().Name}.Added")
{
    public static RepositoryEntityCreatedEvent Create(object data)
    {
        return new RepositoryEntityCreatedEvent(data);
    }
}

public class RepositoryEntityUpdatedEvent(object data) : RepositoryEvent(data, $"{data.GetType().Name}.Updated")
{
    public static RepositoryEntityUpdatedEvent Create(object data)
    {
        return new RepositoryEntityUpdatedEvent(data);
    }
}

public class RepositoryEntityRemovedEvent(object data) : RepositoryEvent(data, $"{data.GetType().Name}.Removed")
{
    public static RepositoryEntityRemovedEvent Create(object data)
    {
        return new RepositoryEntityRemovedEvent(data);
    }
}