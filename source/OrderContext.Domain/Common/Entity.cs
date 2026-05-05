namespace OrderContext.Domain.Common;

/// <summary>
/// Base class for all domain entities (and aggregate roots).
/// Provides domain event collection management.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// The domain events raised by this entity since the last clear.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the entity's internal event collection.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears all collected domain events. Called after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
