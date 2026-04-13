using Microsoft.EntityFrameworkCore.Storage;
using OrderContext.Domain.Repositories;

namespace OrderContext.Infratructure.Repositories;

/// <summary>
/// EF Core implementation of Unit of Work pattern.
/// Coordinates changes and manages transactions across repositories.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    // Lazy-loaded repositories
    private IClientRepository? _clientRepository;

    public UnitOfWork(OrderDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public IClientRepository Clients =>
        _clientRepository ??= new ClientRepository(_context);

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }
}
