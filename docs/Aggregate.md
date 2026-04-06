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
