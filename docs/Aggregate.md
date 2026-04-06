# Aggregate in Domain-Driven Design

> **Branch**: `aggregate-in-ef`

## 📖 What is an Aggregate?

An **Aggregate** is a cluster of domain objects (entities and value objects) that are treated as a single unit for data changes. One entity serves as the **Aggregate Root**, which is the only entry point for accessing the aggregate.

Think of an Aggregate as a **consistency boundary**—all changes within the aggregate must satisfy business rules before being persisted.

### Key Characteristics

| Characteristic | Description |
|----------------|-------------|
| **Aggregate Root** | The single entity through which all access occurs |
| **Consistency Boundary** | Invariants are enforced within the aggregate |
| **Transactional Unit** | Loaded and saved as a whole |
| **Identity** | Referenced externally only by the root's ID |
| **Encapsulation** | Internal objects are not exposed directly |

---

## 🌳 What is an Aggregate Root?

An **Aggregate Root** is the main entity that acts as the gateway to the entire aggregate. All external access to the aggregate must go through the root.

### Aggregate Root Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Identity** | Provides the unique identifier for the entire aggregate |
| **Entry Point** | Only entity accessible from outside the aggregate |
| **Invariant Enforcement** | Ensures all business rules are satisfied |
| **Child Lifecycle** | Controls creation, modification, and deletion of child entities |
| **Consistency** | Guarantees the aggregate is always in a valid state |

### Aggregate Root vs Regular Entity

```
┌─────────────────────────────────────────────────────────┐
│                    AGGREGATE                            │
│  ┌─────────────────────────────────────────────────┐   │
│  │           AGGREGATE ROOT (Client)                │   │
│  │  • Has global identity (Id)                      │   │
│  │  • Accessible from outside                       │   │
│  │  • Enforces invariants                           │   │
│  │  • Controls child entities                       │   │
│  └─────────────────────────────────────────────────┘   │
│           │                                             │
│           ▼                                             │
│  ┌─────────────────┐    ┌─────────────────┐            │
│  │  Value Object   │    │  Child Entity   │            │
│  │    (Email)      │    │   (Address)     │            │
│  │  • No identity  │    │  • Local identity│           │
│  │  • Immutable    │    │  • Only via root │           │
│  └─────────────────┘    └─────────────────┘            │
│                                                         │
│  External code can ONLY access through Aggregate Root   │
└─────────────────────────────────────────────────────────┘
```

### Rules for Aggregate Root

1. **Global Identity**: The root has a unique ID (e.g., `Guid`, `int`)
2. **Single Entry Point**: External objects can only reference the root
3. **Transactional Boundary**: Changes are persisted atomically
4. **Invariant Guardian**: All business rules pass through the root

---

## 👤 Example: Client Aggregate (From This Project)

In our `OrderContext.Domain` project, the `Client` entity serves as an **Aggregate Root** that contains an `Email` **Value Object**.

### Domain Model

#### Value Object: Email

Value Objects are immutable and identified by their attributes, not by an ID:

```csharp
// OrderContext.Domain/Email.cs
public class Email : ValueObject
{
    private static readonly Regex EmailRegex = new Regex(
       @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
       RegexOptions.Compiled | RegexOptions.IgnoreCase
   );

    private readonly string _value;
    public string Value => _value;

    private Email(string value) => _value = value;

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty!");

        email = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException("Invalid email format!");

        if (email.Length > 254)
            throw new ArgumentException("Email exceeds maximum length!");

        return new Email(email);
    }

    // Used by EF Core for materialization - skips validation since data is already validated
    public static Email FromDatabase(string value) => new Email(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
```

#### Aggregate Root: Client

The `Client` entity owns and protects the `Email` value object:

```csharp
// OrderContext.Domain/Client.cs
public class Client
{
    [Key]
    public Guid Id { get; private set; } 
    public string Name { get; private set; }
    public Email Email { get; private set; }  // Value Object
    public DateTime CreatedAt { get; private set; }

    private Client() { }  // EF Core

    private Client(Guid id, string name, Email email)
    {
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    private Client(string name, Email email)
        : this(Guid.NewGuid(), name, email)
    {
    }

    // Factory method enforces invariants at creation
    public static Client Create(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return new Client(name, email);
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty!");
        Name = newName;
    }

    public void UpdateEmail(Email newEmail)
    {
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
    }
}
```

### Why This Design?

| Pattern | Implementation | Benefit |
|---------|----------------|---------|
| **Private Setters** | All properties use `private set` | Prevents external modification |
| **Factory Method** | `Client.Create()` | Centralizes validation and creation logic |
| **Value Object** | `Email` class | Encapsulates email validation rules |
| **Private Constructor** | `private Client()` | Forces use of factory method |

---

## 🏗️ Comprehensive Aggregate Root Implementation

Here's a more complete example showing an Aggregate Root with child entities:

```csharp
// Aggregate Root with child entities
public class Client
{
    // ═══════════════════════════════════════════════════════════════
    // IDENTITY - Global unique identifier
    // ═══════════════════════════════════════════════════════════════
    [Key]
    public Guid Id { get; private set; }

    // ═══════════════════════════════════════════════════════════════
    // STATE - Properties with private setters
    // ═══════════════════════════════════════════════════════════════
    public string Name { get; private set; }
    public Email Email { get; private set; }           // Value Object
    public ClientStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // ═══════════════════════════════════════════════════════════════
    // CHILD ENTITIES - Private collection, public read-only access
    // ═══════════════════════════════════════════════════════════════
    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTORS - Private to force factory method usage
    // ═══════════════════════════════════════════════════════════════
    private Client() { }  // EF Core

    private Client(Guid id, string name, Email email)
    {
        Id = id;
        Name = name;
        Email = email;
        Status = ClientStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTORY METHOD - Single entry point for creation
    // ═══════════════════════════════════════════════════════════════
    public static Client Create(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!", nameof(name));

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return new Client(Guid.NewGuid(), name, email);
    }

    // ═══════════════════════════════════════════════════════════════
    // BEHAVIOR METHODS - Enforce invariants and business rules
    // ═══════════════════════════════════════════════════════════════

    public void UpdateName(string newName)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty!", nameof(newName));

        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(Email newEmail)
    {
        EnsureActive();
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
        UpdatedAt = DateTime.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    // CHILD ENTITY MANAGEMENT - Root controls child lifecycle
    // ═══════════════════════════════════════════════════════════════

    public void AddAddress(string street, string city, string postalCode, AddressType type)
    {
        EnsureActive();

        // Invariant: Only one primary address allowed
        if (type == AddressType.Primary && _addresses.Any(a => a.Type == AddressType.Primary))
            throw new InvalidOperationException("Client already has a primary address.");

        var address = new Address(street, city, postalCode, type);
        _addresses.Add(address);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAddress(Guid addressId)
    {
        EnsureActive();

        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
            throw new InvalidOperationException("Address not found.");

        _addresses.Remove(address);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrimaryAddress(Guid addressId)
    {
        EnsureActive();

        var newPrimary = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new InvalidOperationException("Address not found.");

        // Demote current primary, promote new one
        foreach (var address in _addresses)
        {
            address.SetType(address.Id == addressId 
                ? AddressType.Primary 
                : AddressType.Secondary);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    // STATUS MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    public void Deactivate()
    {
        if (Status == ClientStatus.Inactive)
            throw new InvalidOperationException("Client is already inactive.");

        Status = ClientStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status == ClientStatus.Active)
            throw new InvalidOperationException("Client is already active.");

        Status = ClientStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE HELPER METHODS - Invariant checks
    // ═══════════════════════════════════════════════════════════════

    private void EnsureActive()
    {
        if (Status == ClientStatus.Inactive)
            throw new InvalidOperationException("Cannot modify an inactive client.");
    }
}

// Child Entity - Only accessible through Aggregate Root
public class Address
{
    public Guid Id { get; private set; }
    public string Street { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }
    public AddressType Type { get; private set; }

    private Address() { }  // EF Core

    // Internal constructor - only Client can create
    internal Address(string street, string city, string postalCode, AddressType type)
    {
        Id = Guid.NewGuid();
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        PostalCode = postalCode ?? throw new ArgumentNullException(nameof(postalCode));
        Type = type;
    }

    // Internal method - only Client can modify
    internal void SetType(AddressType type) => Type = type;
}

public enum ClientStatus { Active, Inactive }
public enum AddressType { Primary, Secondary }
```

### Aggregate Root Implementation Checklist

| Component | Purpose | Implementation |
|-----------|---------|----------------|
| **Private setters** | Encapsulation | All properties use `private set` |
| **Private constructor** | Control creation | `private Client() { }` |
| **Factory method** | Enforce creation rules | `Client.Create()` |
| **Private collection** | Protect children | `private readonly List<Address>` |
| **Read-only exposure** | Safe access | `IReadOnlyCollection<Address>` |
| **Internal child constructor** | Root controls lifecycle | `internal Address()` |
| **Behavior methods** | Business logic | `AddAddress()`, `UpdateName()` |
| **Invariant checks** | Business rules | `EnsureActive()`, primary address check |
| **State tracking** | Audit trail | `UpdatedAt` property |

---

## 🎯 Aggregate Best Practices

### 1. Keep Aggregates Small

```
❌ Large Aggregate (avoid)
Order
├── Customer (full entity)
├── OrderItems[]
├── Payments[]
├── Shipments[]
└── Invoices[]

✅ Small Aggregate (preferred)
Order
├── CustomerId (reference by ID)
├── OrderItems[]
└── Status
```

**Rule**: Include only what's needed to enforce invariants within a single transaction.

### 2. Reference Other Aggregates by ID Only

```csharp
// ❌ Bad - direct reference creates tight coupling
public class Order
{
    public Customer Customer { get; private set; }  // Full entity
}

// ✅ Good - reference by ID
public class Order
{
    public Guid CustomerId { get; private set; }  // Just the ID
}
```

### 3. Design Around Business Invariants

| Invariant | Aggregate Design |
|-----------|------------------|
| "Order total cannot exceed credit limit" | Keep `OrderItems` inside `Order` |
| "Email must be valid format" | Use `Email` Value Object inside `Client` |
| "Only one primary address" | Keep `Addresses` inside `Client` |

### 4. One Transaction = One Aggregate

```csharp
// ❌ Bad - modifying multiple aggregates in one transaction
public void PlaceOrder(Order order, Customer customer, Inventory inventory)
{
    order.Confirm();
    customer.AddLoyaltyPoints(100);
    inventory.Reserve(order.Items);
    _dbContext.SaveChanges();  // All in one transaction
}

// ✅ Good - use domain events for eventual consistency
public void PlaceOrder(Order order)
{
    order.Confirm();  // Raises OrderConfirmedEvent
    _dbContext.SaveChanges();
}

// Handle other aggregates via event handlers
public class OrderConfirmedHandler
{
    public void Handle(OrderConfirmedEvent e)
    {
        _customerService.AddLoyaltyPoints(e.CustomerId, 100);
        _inventoryService.Reserve(e.Items);
    }
}
```

### 5. Load Aggregates Completely

```csharp
// ✅ Good - load entire aggregate
var client = await _dbContext.Clients
    .Include(c => c.Addresses)  // If you have child entities
    .FirstOrDefaultAsync(c => c.Id == id);

// ❌ Bad - partial loading breaks invariants
var client = await _dbContext.Clients
    .Select(c => new { c.Name, c.Email })  // Missing data
    .FirstOrDefaultAsync();
```

### 6. Protect Internal State

| Pattern | Implementation | Status |
|---------|----------------|--------|
| Private setters | `public string Name { get; private set; }` | ✅ |
| Private constructor | `private Client() { }` | ✅ |
| Value Objects | `Email` class | ✅ |
| Factory methods | `Client.Create()`, `Email.Create()` | ✅ |

### ✅ Do

- Keep aggregates **small**—prefer smaller boundaries
- Use **Value Objects** for validated, immutable concepts (like Email)
- Use **Factory Methods** to enforce creation invariants
- Reference other aggregates **by ID only**
- Design around **business invariants**

### ❌ Avoid

- Large aggregates that span too many entities
- Public setters on aggregate properties
- Exposing internal collections directly
- Skipping validation in factory methods

---

## 🔗 EF Core Configuration

The `ClientConfiguration` maps the aggregate to the database, including the value object conversion:

```csharp
// OrderContext.Infrastructure/ClientConfiguration.cs
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Configure Email using Value Conversion
        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,                    // To database
                value => Email.FromDatabase(value))     // From database
            .HasMaxLength(254)
            .IsRequired();
    }
}
```

### Key EF Core Patterns

| Pattern | Code | Purpose |
|---------|------|---------|
| **Value Conversion** | `HasConversion()` | Maps `Email` value object to string column |
| **FromDatabase Factory** | `Email.FromDatabase()` | Skips validation when loading (data is already valid) |

---

## 📚 Summary

| Concept | In Our Project |
|---------|----------------|
| Aggregate Root | `Client` |
| Value Object | `Email` |
| Factory Method | `Client.Create()`, `Email.Create()` |
| Invariants | "Name cannot be empty", "Valid email format" |
| EF Core Mapping | Value Conversion for `Email` |

### ✅ Best Practices Checklist

| Practice | Description | Your Project |
|----------|-------------|--------------|
| Small aggregates | Minimal entities per aggregate | ✅ `Client` only |
| Reference by ID | No direct entity references | ✅ N/A yet |
| Factory methods | Centralized creation logic | ✅ `Client.Create()` |
| Value Objects | Immutable validated concepts | ✅ `Email` |
| Private setters | Encapsulated state | ✅ All properties |
| Single transaction | One aggregate per save | ✅ |

Aggregates combined with Value Objects help maintain **consistency**, **validation**, and **encapsulation** in your domain model while providing clear boundaries for transactions and persistence.
