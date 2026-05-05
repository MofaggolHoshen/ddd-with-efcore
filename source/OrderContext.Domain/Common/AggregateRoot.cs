namespace OrderContext.Domain.Common;

/// <summary>
/// Base class for aggregate roots.
/// Aggregate roots are entities that can raise domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// The domain events raised by this aggregate root since the last clear.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate root's internal event collection.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears all collected domain events. Called after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
