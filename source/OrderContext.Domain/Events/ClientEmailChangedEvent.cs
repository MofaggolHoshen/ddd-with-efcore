using OrderContext.Domain.Common;

namespace OrderContext.Domain.Events;

/// <summary>
/// Raised when a client's email address is updated.
/// </summary>
public sealed class ClientEmailChangedEvent : IDomainEvent
{
    public Guid ClientId { get; }
    public string OldEmail { get; }
    public string NewEmail { get; }
    public DateTime OccurredOn { get; }

    public ClientEmailChangedEvent(Guid clientId, string oldEmail, string newEmail)
    {
        ClientId = clientId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
        OccurredOn = DateTime.UtcNow;
    }
}
