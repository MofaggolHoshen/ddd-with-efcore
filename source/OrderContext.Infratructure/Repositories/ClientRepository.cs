using Microsoft.EntityFrameworkCore;
using OrderContext.Domain;
using OrderContext.Domain.Repositories;

namespace OrderContext.Infratructure.Repositories;

/// <summary>
/// EF Core implementation of IClientRepository.
/// Provides data access for the Client aggregate root.
/// </summary>
public sealed class ClientRepository : Repository<Client, Guid>, IClientRepository
{
    public ClientRepository(OrderDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public override async Task<Client?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Client?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Email.Value == email.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(c => c.Email.Value == email.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Client>> GetClientsCreatedBetweenAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await DbSet.CountAsync(cancellationToken);

        var items = await DbSet
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Client>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Name.Contains(searchTerm))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
