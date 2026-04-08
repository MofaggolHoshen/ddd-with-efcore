namespace OrderContext.Domain.Services;

/// <summary>
/// Domain service interface for handling complex client operations
/// that span multiple entities or require business validation.
/// </summary>
public interface IClientTransferService
{
    /// <summary>
    /// Updates a client's email with uniqueness validation.
    /// </summary>
    /// <param name="client">The client to update</param>
    /// <param name="newEmail">The new email address</param>
    void UpdateClientEmail(Client client, Email newEmail);
}
