using OrderContext.Application.DTOs;
using OrderContext.Domain;
using OrderContext.Domain.Repositories;

namespace OrderContext.Application.Services;

/// <summary>
/// Application service for client-related operations.
/// Coordinates between the domain layer and infrastructure.
/// </summary>
public class ClientApplicationService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClientApplicationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Registers a new client.
    /// </summary>
    /// <param name="name">The name of the client.</param>
    /// <param name="emailAddress">The email address of the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unique identifier of the newly created client.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a client with the same email already exists.</exception>
    public async Task<Guid> RegisterClientAsync(
        string name,
        string emailAddress,
        CancellationToken cancellationToken = default)
    {
        // Create domain objects
        var email = Email.Create(emailAddress);

        // Check if email already exists
        if (await _unitOfWork.Clients.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException($"A client with email '{emailAddress}' already exists.");
        }

        var client = Client.Create(name, email);

        // Use repository through Unit of Work
        await _unitOfWork.Clients.AddAsync(client, cancellationToken);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return client.Id;
    }

    /// <summary>
    /// Gets a client by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The client DTO if found; otherwise, null.</returns>
    public async Task<ClientDto?> GetClientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            return null;
        }

        return MapToDto(client);
    }

    /// <summary>
    /// Gets all clients.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of all client DTOs.</returns>
    public async Task<IReadOnlyList<ClientDto>> GetAllClientsAsync(
        CancellationToken cancellationToken = default)
    {
        var clients = await _unitOfWork.Clients.GetAllAsync(cancellationToken);

        return clients.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Updates a client's name.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="newName">The new name for the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the client is not found.</exception>
    public async Task UpdateClientNameAsync(
        Guid id,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            throw new InvalidOperationException($"Client with id '{id}' not found.");
        }

        client.UpdateName(newName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates a client's email.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="newEmailAddress">The new email address for the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the client is not found or email already exists.</exception>
    public async Task UpdateClientEmailAsync(
        Guid id,
        string newEmailAddress,
        CancellationToken cancellationToken = default)
    {
        var newEmail = Email.Create(newEmailAddress);

        // Check if new email already exists for another client
        var existingClient = await _unitOfWork.Clients.GetByEmailAsync(newEmail, cancellationToken);
        if (existingClient != null && existingClient.Id != id)
        {
            throw new InvalidOperationException($"A client with email '{newEmailAddress}' already exists.");
        }

        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            throw new InvalidOperationException($"Client with id '{id}' not found.");
        }

        client.UpdateEmail(newEmail);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a client.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the client is not found.</exception>
    public async Task DeleteClientAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            throw new InvalidOperationException($"Client with id '{id}' not found.");
        }

        _unitOfWork.Clients.Remove(client);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets clients created within a date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of client DTOs created within the date range.</returns>
    public async Task<IReadOnlyList<ClientDto>> GetClientsCreatedBetweenAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var clients = await _unitOfWork.Clients.GetClientsCreatedBetweenAsync(
            startDate, endDate, cancellationToken);

        return clients.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets clients with pagination support.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of client DTOs.</returns>
    public async Task<PagedResult<ClientDto>> GetPagedClientsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (clients, totalCount) = await _unitOfWork.Clients.GetPagedAsync(
            page, pageSize, cancellationToken);

        var items = clients.Select(MapToDto).ToList();

        return new PagedResult<ClientDto>(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// Searches clients by name.
    /// </summary>
    /// <param name="searchTerm">The search term to match against client names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of client DTOs matching the search term.</returns>
    public async Task<IReadOnlyList<ClientDto>> SearchClientsByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var clients = await _unitOfWork.Clients.SearchByNameAsync(
            searchTerm, cancellationToken);

        return clients.Select(MapToDto).ToList();
    }

    private static ClientDto MapToDto(Client client)
    {
        return new ClientDto(
            client.Id,
            client.Name,
            client.Email.Value,
            client.CreatedAt
        );
    }
}
