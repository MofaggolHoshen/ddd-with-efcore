namespace OrderContext.Domain.Common;

/// <summary>
/// Marker contract for aggregate roots.
/// Aggregate roots are the only entities allowed to emit domain events.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
