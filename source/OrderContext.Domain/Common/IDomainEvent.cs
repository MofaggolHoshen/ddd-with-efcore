namespace OrderContext.Domain.Common;

/// <summary>
/// Marker interface for all domain events.
/// A domain event represents something significant that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>The UTC timestamp when the event occurred.</summary>
    DateTime OccurredOn { get; }
}
