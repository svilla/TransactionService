using System;

namespace AntiFraudService.Domain.Events;

/// <summary>
/// Base class for all domain events in the system.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; protected set; }

    /// <summary>
    /// Gets the timestamp when this event occurred.
    /// </summary>
    public DateTime Timestamp { get; protected set; }

    /// <summary>
    /// Gets the event type name, used for routing and serialization.
    /// </summary>
    public string EventType => GetType().Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }
} 