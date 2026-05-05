namespace OrderContext.Domain.Common;

/// <summary>
/// Dispatches domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
