namespace OrderContext.Domain.Repositories;

/// <summary>
/// Repository interface for the Client aggregate root.
/// Provides collection-like access to Client entities with domain-specific queries.
/// </summary>
public interface IClientRepository : IRepository<Client, Guid>
{
    /// <summary>
    /// Retrieves a client by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The client if found; otherwise, null.</returns>
    Task<Client?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a client with the specified email exists.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a client exists; otherwise, false.</returns>
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves clients created within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of clients created within the date range.</returns>
    Task<IReadOnlyList<Client>> GetClientsCreatedBetweenAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves clients with pagination support.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the items and total count for pagination.</returns>
    Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches clients by name.
    /// </summary>
    /// <param name="searchTerm">The search term to match against client names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of clients matching the search term.</returns>
    Task<IReadOnlyList<Client>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);
}
