using OrderContext.Domain.Common;

namespace OrderContext.Domain.Events;

/// <summary>
/// Raised when a new client is registered in the system.
/// </summary>
public sealed class ClientRegisteredEvent : IDomainEvent
{
    public Guid ClientId { get; }
    public string Name { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public ClientRegisteredEvent(Guid clientId, string name, string email)
    {
        ClientId = clientId;
        Name = name;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}
