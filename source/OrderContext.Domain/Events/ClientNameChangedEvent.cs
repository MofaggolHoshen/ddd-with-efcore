using OrderContext.Domain.Common;

namespace OrderContext.Domain.Events;

/// <summary>
/// Raised when a client's name is updated.
/// </summary>
public sealed class ClientNameChangedEvent : IDomainEvent
{
    public Guid ClientId { get; }
    public string OldName { get; }
    public string NewName { get; }
    public DateTime OccurredOn { get; }

    public ClientNameChangedEvent(Guid clientId, string oldName, string newName)
    {
        ClientId = clientId;
        OldName = oldName;
        NewName = newName;
        OccurredOn = DateTime.UtcNow;
    }
}
