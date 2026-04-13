# Repository Pattern in Domain-Driven Design

> **Status**: ✅ Complete  
> **Branch**: `repository-in-ef`

## 📖 Table of Contents

- [What is a Repository?](#-what-is-a-repository)
- [Repository Pattern Fundamentals](#-repository-pattern-fundamentals)
- [Key Principles](#-key-principles)
- [Repository Interface Design](#-repository-interface-design)
- [Generic vs Specific Repositories](#-generic-vs-specific-repositories)
- [Implementing with EF Core](#-implementing-with-ef-core)
- [Unit of Work Pattern](#-unit-of-work-pattern)
- [Query vs Command Methods (CQRS)](#-query-vs-command-methods-cqrs)
- [Best Practices](#-best-practices)
- [Common Pitfalls to Avoid](#-common-pitfalls-to-avoid)
- [Testing Repositories](#-testing-repositories)

---

## 📖 What is a Repository?

A **Repository** is a design pattern that mediates between the domain and data mapping layers, acting like an **in-memory collection of domain objects**. It provides a clean abstraction for data access, allowing the domain layer to remain ignorant of persistence concerns.

> "A Repository mediates between the domain and data mapping layers, acting like an in-memory domain object collection."  
> — Eric Evans, Domain-Driven Design

### The Core Idea

Think of a Repository as a **specialized collection** that knows how to:
- **Find** aggregates by identity or criteria
- **Add** new aggregates to the collection
- **Remove** aggregates from the collection
- **Persist** changes transparently

```
┌─────────────────────────────────────────────────────────────────┐
│                      Application Layer                          │
│                                                                  │
│    ┌──────────────────┐     ┌──────────────────┐                │
│    │  Application     │     │   Domain         │                │
│    │  Services        │────▶│   Services       │                │
│    └────────┬─────────┘     └──────────────────┘                │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │  IRepository     │  ◀── Interface (Domain Layer)           │
│    │  (Abstraction)   │                                         │
│    └────────┬─────────┘                                         │
└─────────────┼───────────────────────────────────────────────────┘
              │
┌─────────────┼───────────────────────────────────────────────────┐
│             ▼                       Infrastructure Layer         │
│    ┌──────────────────┐                                         │
│    │  Repository      │  ◀── Implementation                     │
│    │  (EF Core)       │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │   DbContext      │                                         │
│    │   (EF Core)      │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │    Database      │                                         │
│    └──────────────────┘                                         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Repository Pattern Fundamentals

### Why Use the Repository Pattern?

| Benefit | Description |
|---------|-------------|
| **Abstraction** | Hides data access implementation details from the domain |
| **Testability** | Easy to mock or stub for unit testing |
| **Flexibility** | Switch persistence technologies without affecting domain logic |
| **Consistency** | Enforces aggregate boundaries and invariants |
| **Centralization** | Single point of data access logic |

### Repository vs Direct DbContext Access

```csharp
// ❌ Without Repository - Domain coupled to EF Core
public class ClientService
{
    private readonly OrderDbContext _context;

    public async Task<Client> GetClient(Guid id)
    {
        // Domain layer knows about EF Core specifics
        return await _context.Clients
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}

// ✅ With Repository - Clean separation
public class ClientService
{
    private readonly IClientRepository _repository;

    public async Task<Client> GetClient(Guid id)
    {
        // Domain layer only knows the abstraction
        return await _repository.GetByIdAsync(id);
    }
}
```

---

## 🔑 Key Principles

### 1. One Repository Per Aggregate Root

Repositories should only exist for **Aggregate Roots**, not for every entity. Child entities are accessed through their aggregate root.

```csharp
// ✅ Correct - Repository for Aggregate Root
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(Guid id);  // Returns Order with OrderItems
}

// ❌ Wrong - Repository for child entity
public interface IOrderItemRepository  // OrderItem is not an Aggregate Root!
{
    Task<OrderItem> GetByIdAsync(Guid id);
}
```

### 2. Collection-Like Interface

Repositories should feel like working with an in-memory collection:

```csharp
public interface IClientRepository
{
    // Like collection.Find()
    Task<Client?> GetByIdAsync(Guid id);

    // Like collection.Where()
    Task<IReadOnlyList<Client>> FindByEmailDomainAsync(string domain);

    // Like collection.Add()
    Task AddAsync(Client client);

    // Like collection.Remove()
    void Remove(Client client);
}
```

### 3. Return Complete Aggregates

Repositories should return fully-formed aggregates with all necessary related data loaded:

```csharp
public class ClientRepository : IClientRepository
{
    public async Task<Client?> GetByIdAsync(Guid id)
    {
        // Returns the complete aggregate
        return await _context.Clients
            .Include(c => c.Email)      // Load Value Objects
            .Include(c => c.Orders)     // Load child entities if part of aggregate
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
```

### 4. Persistence Ignorance in Domain Layer

The domain layer should not know HOW data is persisted:

```csharp
// Domain Layer - Only defines the contract
namespace OrderContext.Domain.Repositories;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);
    Task AddAsync(Client client);
    void Remove(Client client);
}

// Infrastructure Layer - Implements the contract
namespace OrderContext.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly OrderDbContext _context;

    // Implementation using EF Core
}
```

---

## 📋 Repository Interface Design

### Basic Repository Interface

Here's a well-designed repository interface for the `Client` aggregate:

```csharp
namespace OrderContext.Domain.Repositories;

/// <summary>
/// Repository interface for the Client aggregate root.
/// Provides collection-like access to Client entities.
/// </summary>
public interface IClientRepository
{
    /// <summary>
    /// Retrieves a client by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The client if found; otherwise, null.</returns>
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a client by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The client if found; otherwise, null.</returns>
    Task<Client?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all clients.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of all clients.</returns>
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a client with the specified email exists.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a client exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new client to the repository.
    /// </summary>
    /// <param name="client">The client to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Client client, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing client in the repository.
    /// Note: With EF Core change tracking, explicit update may not be needed.
    /// </summary>
    /// <param name="client">The client to update.</param>
    void Update(Client client);

    /// <summary>
    /// Removes a client from the repository.
    /// </summary>
    /// <param name="client">The client to remove.</param>
    void Remove(Client client);
}
```

### Domain-Specific Query Methods

Add methods that reflect domain concepts, not technical queries:

```csharp
public interface IClientRepository
{
    // ✅ Domain-focused method names
    Task<IReadOnlyList<Client>> GetActiveClientsAsync();
    Task<IReadOnlyList<Client>> GetClientsCreatedAfterAsync(DateTime date);
    Task<Client?> FindByEmailAsync(Email email);

    // ❌ Avoid generic query methods that expose implementation
    // IQueryable<Client> Query();  // Leaks EF Core abstraction
}
```

---

## 🔄 Generic vs Specific Repositories

### Generic Repository (Base)

A generic repository provides common operations that can be reused:

```csharp
namespace OrderContext.Domain.Repositories;

/// <summary>
/// Generic repository interface providing basic CRUD operations.
/// </summary>
/// <typeparam name="T">The aggregate root type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public interface IRepository<T, TId> where T : class
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
```

### Generic Repository Implementation

```csharp
namespace OrderContext.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation using EF Core.
/// </summary>
public abstract class Repository<T, TId> : IRepository<T, TId> 
    where T : class
{
    protected readonly OrderDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected Repository(OrderDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
```

### Specific Repository (Recommended)

Extend the generic repository with domain-specific methods:

```csharp
namespace OrderContext.Domain.Repositories;

/// <summary>
/// Specific repository interface for Client aggregate with domain-specific queries.
/// </summary>
public interface IClientRepository : IRepository<Client, Guid>
{
    Task<Client?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> GetClientsCreatedBetweenAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
}
```

### Specific Repository Implementation

```csharp
namespace OrderContext.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IClientRepository.
/// </summary>
public class ClientRepository : Repository<Client, Guid>, IClientRepository
{
    public ClientRepository(OrderDbContext context) : base(context)
    {
    }

    public override async Task<Client?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        // Override to include related data
        return await DbSet
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Client?> GetByEmailAsync(
        Email email, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        Email email, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(c => c.Email == email, cancellationToken);
    }

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
}
```

### When to Use Each Approach

| Approach | Use When |
|----------|----------|
| **Generic Only** | Simple CRUD operations, rapid prototyping |
| **Specific Only** | Complex domain with unique query requirements |
| **Generic + Specific** | Balance of reusability and domain specificity (Recommended) |

---

## 🛠️ Implementing with EF Core

### Project Structure

```
OrderContext.Domain/
├── Repositories/
│   ├── IRepository.cs           # Generic interface
│   └── IClientRepository.cs     # Specific interface
├── Client.cs                    # Aggregate Root
└── Email.cs                     # Value Object

OrderContext.Infrastructure/
├── Repositories/
│   ├── Repository.cs            # Generic implementation
│   └── ClientRepository.cs      # Specific implementation
├── OrderDbContext.cs
└── Configurations/
    └── ClientConfiguration.cs
```

### Complete Repository Implementation

```csharp
using Microsoft.EntityFrameworkCore;
using OrderContext.Domain.Repositories;

namespace OrderContext.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the Client repository.
/// Provides data access for the Client aggregate root.
/// </summary>
public sealed class ClientRepository : IClientRepository
{
    private readonly OrderDbContext _context;

    public ClientRepository(OrderDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Client?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Client?> GetByEmailAsync(
        Email email, 
        CancellationToken cancellationToken = default)
    {
        // Compare email value since Email is a Value Object
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.Email.Value == email.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()  // Read-only queries don't need tracking
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Email email, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AnyAsync(c => c.Email.Value == email.Value, cancellationToken);
    }

    public async Task AddAsync(
        Client client, 
        CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
    }

    public void Update(Client client)
    {
        // EF Core tracks changes automatically when entity is retrieved via the context
        // Explicit Update is only needed for detached entities
        _context.Clients.Update(client);
    }

    public void Remove(Client client)
    {
        _context.Clients.Remove(client);
    }
}
```

### Registering Repositories with Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using OrderContext.Domain.Repositories;
using OrderContext.Infrastructure.Repositories;

namespace OrderContext.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        string connectionString)
    {
        // Register DbContext
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register Repositories
        services.AddScoped<IClientRepository, ClientRepository>();
        // Add more repositories as needed

        return services;
    }
}
```

---

## 🔗 Unit of Work Pattern

The **Unit of Work** pattern tracks changes made during a business transaction and coordinates the writing of these changes to the database.

### Why Unit of Work?

- **Atomic Operations**: Ensures all changes succeed or fail together
- **Transaction Management**: Handles database transactions transparently
- **Change Tracking**: Tracks all modifications to entities
- **Performance**: Batches multiple changes into a single database round-trip

### Unit of Work Interface

```csharp
namespace OrderContext.Domain.Repositories;

/// <summary>
/// Unit of Work interface for managing transactions across repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the client repository.
    /// </summary>
    IClientRepository Clients { get; }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### Unit of Work Implementation

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderContext.Domain.Repositories;

namespace OrderContext.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of Unit of Work pattern.
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

    public IClientRepository Clients => 
        _clientRepository ??= new ClientRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);
    }

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
```

### Using Unit of Work in Application Services

```csharp
namespace OrderContext.Application.Services;

public class ClientApplicationService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClientApplicationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> RegisterClientAsync(
        string name, 
        string emailAddress,
        CancellationToken cancellationToken = default)
    {
        // Create domain objects
        var email = Email.Create(emailAddress);
        var client = Client.Create(name, email);

        // Use repository through Unit of Work
        await _unitOfWork.Clients.AddAsync(client, cancellationToken);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return client.Id;
    }

    public async Task TransferClientsAsync(
        Guid sourceClientId,
        Guid targetClientId,
        CancellationToken cancellationToken = default)
    {
        // Begin transaction for complex operations
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var sourceClient = await _unitOfWork.Clients
                .GetByIdAsync(sourceClientId, cancellationToken);
            var targetClient = await _unitOfWork.Clients
                .GetByIdAsync(targetClientId, cancellationToken);

            if (sourceClient == null || targetClient == null)
            {
                throw new InvalidOperationException("Client not found.");
            }

            // Perform business operations...
            // Both changes will be committed together

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

---

## 📊 Query vs Command Methods (CQRS)

**CQRS (Command Query Responsibility Segregation)** separates read and write operations, which can be applied to repositories.

### The Concept

| Type | Purpose | Characteristics |
|------|---------|-----------------|
| **Query** | Read data | No side effects, can use `AsNoTracking()` |
| **Command** | Modify data | Changes state, requires change tracking |

### Separating Query and Command Repositories

```csharp
// Query Repository - Read operations only
namespace OrderContext.Domain.Repositories.Queries;

public interface IClientQueryRepository
{
    Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientDto>> SearchByNameAsync(
        string searchTerm, 
        CancellationToken cancellationToken = default);
    Task<PagedResult<ClientDto>> GetPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);
}

// Command Repository - Write operations only
namespace OrderContext.Domain.Repositories.Commands;

public interface IClientCommandRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    void Update(Client client);
    void Remove(Client client);
}
```

### DTOs for Query Results

```csharp
namespace OrderContext.Application.DTOs;

/// <summary>
/// Read-only DTO for client queries.
/// </summary>
public record ClientDto(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAt
);

/// <summary>
/// Paginated result for queries.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

### Query Repository Implementation

```csharp
namespace OrderContext.Infrastructure.Repositories.Queries;

public class ClientQueryRepository : IClientQueryRepository
{
    private readonly OrderDbContext _context;

    public ClientQueryRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<ClientDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()  // No change tracking for queries
            .Where(c => c.Id == id)
            .Select(c => new ClientDto(
                c.Id,
                c.Name,
                c.Email.Value,
                c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .Select(c => new ClientDto(
                c.Id,
                c.Name,
                c.Email.Value,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientDto>> SearchByNameAsync(
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .Where(c => c.Name.Contains(searchTerm))
            .Select(c => new ClientDto(
                c.Id,
                c.Name,
                c.Email.Value,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ClientDto>> GetPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Clients.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClientDto(
                c.Id,
                c.Name,
                c.Email.Value,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ClientDto>(items, totalCount, page, pageSize);
    }
}
```

---

## ✅ Best Practices

### 1. Use Async Methods

```csharp
// ✅ Async for I/O operations
Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

// ❌ Avoid sync methods for database operations
Client? GetById(Guid id);
```

### 2. Support Cancellation Tokens

```csharp
public async Task<Client?> GetByIdAsync(
    Guid id, 
    CancellationToken cancellationToken = default)  // Allow cancellation
{
    return await _context.Clients
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
```

### 3. Return Appropriate Types

```csharp
// ✅ Return nullable for single items (might not exist)
Task<Client?> GetByIdAsync(Guid id);

// ✅ Return IReadOnlyList for collections (immutable)
Task<IReadOnlyList<Client>> GetAllAsync();

// ❌ Avoid returning IQueryable (leaks implementation)
IQueryable<Client> Query();

// ❌ Avoid returning List directly (mutable)
Task<List<Client>> GetAllAsync();
```

### 4. Handle Null Aggregates Appropriately

```csharp
public class ClientApplicationService
{
    public async Task<ClientDto> GetClientOrThrowAsync(Guid id)
    {
        var client = await _repository.GetByIdAsync(id);

        if (client == null)
        {
            throw new ClientNotFoundException(id);
        }

        return MapToDto(client);
    }
}
```

### 5. Use Specifications for Complex Queries

```csharp
// Specification pattern for reusable query logic
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
}

// Repository method accepting specifications
Task<IReadOnlyList<T>> ListAsync(
    ISpecification<T> spec, 
    CancellationToken cancellationToken = default);
```

### 6. Avoid Business Logic in Repositories

```csharp
// ❌ Wrong - Business logic in repository
public class ClientRepository
{
    public async Task RegisterClientAsync(string name, string email)
    {
        // Validation logic doesn't belong here
        if (await EmailExistsAsync(email))
            throw new Exception("Email exists");

        var client = Client.Create(name, email);
        await AddAsync(client);
        await _context.SaveChangesAsync();  // Should be in Unit of Work
    }
}

// ✅ Correct - Repository only handles data access
public class ClientRepository
{
    public async Task<bool> EmailExistsAsync(Email email) => ...;
    public async Task AddAsync(Client client) => ...;
}

// Business logic in Application/Domain Service
public class ClientRegistrationService
{
    public async Task<Client> RegisterAsync(string name, Email email)
    {
        if (await _repository.EmailExistsAsync(email))
            throw new EmailAlreadyExistsException(email);

        var client = Client.Create(name, email);
        await _repository.AddAsync(client);
        await _unitOfWork.SaveChangesAsync();

        return client;
    }
}
```

---

## ⚠️ Common Pitfalls to Avoid

### 1. Exposing IQueryable

```csharp
// ❌ Leaks EF Core abstraction to domain layer
public interface IClientRepository
{
    IQueryable<Client> Query();
}

// ✅ Encapsulate queries within repository
public interface IClientRepository
{
    Task<IReadOnlyList<Client>> FindByCreatedDateAsync(DateTime date);
}
```

### 2. Repository Per Entity (Instead of Per Aggregate)

```csharp
// ❌ Wrong - Repositories for non-aggregate entities
public interface IOrderItemRepository { }  // OrderItem is part of Order aggregate

// ✅ Correct - Access child entities through aggregate
public interface IOrderRepository
{
    Task<Order> GetByIdWithItemsAsync(Guid id);  // Includes OrderItems
}
```

### 3. Saving in Repository Methods

```csharp
// ❌ Wrong - Repository calls SaveChanges
public class ClientRepository
{
    public async Task AddAsync(Client client)
    {
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();  // Should be in Unit of Work
    }
}

// ✅ Correct - Unit of Work handles persistence
public class ClientRepository
{
    public async Task AddAsync(Client client)
    {
        await _context.Clients.AddAsync(client);
        // SaveChanges called by Unit of Work
    }
}
```

### 4. Generic Repository Without Specific Methods

```csharp
// ❌ Forces consumers to use generic methods for everything
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}

// ✅ Add domain-specific methods
public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetByEmailAsync(Email email);  // Domain-specific
    Task<IReadOnlyList<Client>> GetVipClientsAsync();  // Domain-specific
}
```

### 5. Not Using AsNoTracking for Read-Only Queries

```csharp
// ❌ Unnecessary change tracking for read-only data
public async Task<IReadOnlyList<ClientDto>> GetAllAsync()
{
    return await _context.Clients
        .Select(c => new ClientDto(...))
        .ToListAsync();
}

// ✅ Improved performance with AsNoTracking
public async Task<IReadOnlyList<ClientDto>> GetAllAsync()
{
    return await _context.Clients
        .AsNoTracking()  // Skip change tracking
        .Select(c => new ClientDto(...))
        .ToListAsync();
}
```

---

## 🧪 Testing Repositories

### Unit Testing with In-Memory Database

```csharp
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OrderContext.Tests.Repositories;

public class ClientRepositoryTests
{
    private readonly OrderDbContext _context;
    private readonly ClientRepository _repository;

    public ClientRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _repository = new ClientRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingClient_ReturnsClient()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var client = Client.Create("Test Client", email);
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(client.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(client.Id, result.Id);
        Assert.Equal("Test Client", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingClient_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ValidClient_ClientIsPersisted()
    {
        // Arrange
        var email = Email.Create("new@example.com");
        var client = Client.Create("New Client", email);

        // Act
        await _repository.AddAsync(client);
        await _context.SaveChangesAsync();

        // Assert
        var persisted = await _context.Clients.FindAsync(client.Id);
        Assert.NotNull(persisted);
        Assert.Equal("New Client", persisted.Name);
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var email = Email.Create("existing@example.com");
        var client = Client.Create("Existing Client", email);
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(email);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task Remove_ExistingClient_ClientIsDeleted()
    {
        // Arrange
        var email = Email.Create("delete@example.com");
        var client = Client.Create("Delete Me", email);
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act
        _repository.Remove(client);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Clients.FindAsync(client.Id);
        Assert.Null(deleted);
    }
}
```

### Mocking Repositories for Service Tests

```csharp
using Moq;
using Xunit;

namespace OrderContext.Tests.Services;

public class ClientApplicationServiceTests
{
    private readonly Mock<IClientRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ClientApplicationService _service;

    public ClientApplicationServiceTests()
    {
        _repositoryMock = new Mock<IClientRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock
            .Setup(u => u.Clients)
            .Returns(_repositoryMock.Object);

        _service = new ClientApplicationService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task RegisterClientAsync_ValidData_ReturnsClientId()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RegisterClientAsync("Test", "test@example.com");

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
```

---

## 📚 Summary

| Concept | Key Points |
|---------|------------|
| **Repository** | Abstraction for aggregate persistence; acts like an in-memory collection |
| **Aggregate Root** | Only aggregate roots have repositories |
| **Generic Repository** | Provides reusable CRUD operations |
| **Specific Repository** | Adds domain-specific query methods |
| **Unit of Work** | Coordinates changes and transactions across repositories |
| **CQRS** | Separates read (query) and write (command) responsibilities |

### Quick Reference

```csharp
// 1. Define interface in Domain Layer
public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Client client, CancellationToken ct = default);
    void Remove(Client client);
}

// 2. Implement in Infrastructure Layer
public class ClientRepository : IClientRepository
{
    private readonly OrderDbContext _context;
    // Implementation...
}

// 3. Use Unit of Work for transactions
public interface IUnitOfWork
{
    IClientRepository Clients { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// 4. Register with DI
services.AddScoped<IClientRepository, ClientRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## 🔗 Related Topics

- [Aggregates](./Aggregate.md) - Understanding aggregate roots
- [Value Objects](./ValueObject.md) - Immutable domain concepts
- [Domain Services](./DomainServices.md) - Business logic coordination
- [Entity Framework Core Configuration](./EFCoreConfiguration.md) - Mapping aggregates to database

---

📌 **Next Steps**: Implement the repository pattern in your domain by creating interfaces in the Domain layer and implementations in the Infrastructure layer. Start with a simple specific repository and add generic base classes as patterns emerge.
