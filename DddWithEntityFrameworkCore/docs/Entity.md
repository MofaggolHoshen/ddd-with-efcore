# Entity in Domain-Driven Design

> **Branch**: `entity-in-ef`  
> **Status**: ✅ Implemented

## 📖 What is an Entity?

An **Entity** is a domain object that has a **unique identity** that runs through time and different representations. Unlike value objects, two entities with identical properties but different identities are considered different objects.

### Key Characteristics

1. **Unique Identity**: Has an identifier (ID) that distinguishes it from other instances
2. **Mutable**: Can change state over time through well-defined methods
3. **Identity-based Equality**: Two entities are equal if their IDs match, not their attributes
4. **Lifecycle**: Has a lifespan from creation to deletion
5. **Encapsulation**: Protects its internal state and invariants

## 🎯 Entity vs Value Object

| Aspect | Entity | Value Object |
|--------|--------|--------------|
| Identity | Has unique ID | No identity |
| Mutability | Mutable | Immutable |
| Equality | By ID | By value |
| Lifecycle | Has lifecycle | Replaceable |
| Example | Client, Order, Product | Email, Money, Address |

## 💻 Implementation in This Project

### The Client Entity

```csharp
public class Client
{
    // 1. IDENTITY - Unique identifier
    [Key]
    public Guid Id { get; private set; }

    // 2. ATTRIBUTES - Mutable state
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // 3. PRIVATE CONSTRUCTOR - For EF Core
    private Client()
    {
    }

    // 4. PRIVATE CONSTRUCTOR - For domain logic
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

    // 5. FACTORY METHOD - Controlled creation with validation
    public static Client Create(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return new Client(name, email);
    }

    // 6. BEHAVIOR METHODS - Encapsulated state changes
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

## 🔍 Design Patterns Applied

### 1. Encapsulation

**Problem**: Direct property access allows invalid state

```csharp
// ❌ BAD - Public setters allow invalid state
public class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

// Usage - Can create invalid client
var client = new Client { Name = "" }; // Invalid!
```

**Solution**: Private setters + behavior methods

```csharp
// ✅ GOOD - Encapsulated with validation
public class Client
{
    public string Name { get; private set; }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty!");

        Name = newName;
    }
}
```

### 2. Factory Method Pattern

**Problem**: Constructors can't prevent invalid object creation

```csharp
// ❌ BAD - Public constructor, no validation enforcement
var client = new Client("", null); // Compiles but invalid!
```

**Solution**: Private constructor + static factory method

```csharp
// ✅ GOOD - Factory method ensures valid creation
private Client(string name, Email email) { ... }

public static Client Create(string name, Email email)
{
    // Validation here ensures only valid clients are created
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be empty!");

    return new Client(name, email);
}

// Usage
var client = Client.Create("John Doe", email); // Always valid!
```

### 3. Tell, Don't Ask Principle

**Problem**: Client code manipulates entity's data

```csharp
// ❌ BAD - Violates encapsulation
if (client.Name != newName)
{
    client.Name = newName;
    client.UpdatedAt = DateTime.UtcNow;
}
```

**Solution**: Entity manages its own state

```csharp
// ✅ GOOD - Tell the entity what to do
client.UpdateName(newName);

// Inside UpdateName method
public void UpdateName(string newName)
{
    if (string.IsNullOrWhiteSpace(newName))
        throw new ArgumentException("Name cannot be empty!");

    Name = newName;
    // Could also update UpdatedAt here if needed
}
```

## 🗄️ Entity Framework Core Configuration

### ClientConfiguration.cs

```csharp
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        // 1. Primary Key Configuration
        builder.HasKey(c => c.Id);

        // 2. Value Object Mapping (Email is owned by Client)
        builder.OwnsOne<Email>(c => c.Email);

        // 3. Required Navigation
        builder.Navigation(c => c.Email).IsRequired();
    }
}
```

### Key Points

1. **`HasKey(c => c.Id)`**: Configures the entity's primary key
2. **`OwnsOne<Email>`**: Maps Email value object inline (no separate table)
3. **`IsRequired()`**: Email must always have a value

### Database Schema Result

```sql
-- Single table for Client with Email columns inline
CREATE TABLE Clients (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(MAX),
    Email_Value NVARCHAR(MAX),  -- Email value object property
    CreatedAt DATETIME2
)
```

## ✅ Best Practices Demonstrated

### 1. Always Valid State

```csharp
// ✅ Cannot create invalid client
var client = Client.Create("", email); // Throws ArgumentException
```

The entity enforces its invariants (business rules) at all times.

### 2. Private Setters

```csharp
public string Name { get; private set; }
```

Prevents external code from bypassing validation.

### 3. Meaningful Methods

```csharp
// ✅ Clear intent
client.UpdateName("New Name");

// ❌ Less clear
client.Name = "New Name"; // If setter was public
```

Methods express business operations, not just data changes.

### 4. Constructor Overloading for Different Scenarios

```csharp
private Client() { }  // For EF Core hydration

private Client(string name, Email email)  // For new creation
    : this(Guid.NewGuid(), name, email)
{
}

private Client(Guid id, string name, Email email)  // For reconstruction
{
    Id = id;
    Name = name;
    Email = email;
    CreatedAt = DateTime.UtcNow;
}
```

## 🎓 Common Mistakes to Avoid

### ❌ Mistake 1: Anemic Domain Model

```csharp
// BAD - Just a data container
public class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Business logic lives elsewhere (in services)
public class ClientService
{
    public void UpdateClient(Client client, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException();
        client.Name = name;
    }
}
```

**Why it's bad**: Business logic is scattered, no encapsulation

### ✅ Correct Approach: Rich Domain Model

```csharp
// GOOD - Entity contains business logic
public class Client
{
    public string Name { get; private set; }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException();
        Name = newName;
    }
}
```

### ❌ Mistake 2: Public Setters

```csharp
// BAD - Allows invalid state
public Email Email { get; set; }

// Somewhere in code
client.Email = null; // No validation!
```

### ✅ Correct Approach: Controlled Mutation

```csharp
// GOOD - Validation enforced
public Email Email { get; private set; }

public void UpdateEmail(Email newEmail)
{
    Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
}
```

### ❌ Mistake 3: Identity Generation in Constructor

```csharp
// BAD - EF Core can't hydrate from database
public Client(string name, Email email)
{
    Id = Guid.NewGuid(); // Always generates new ID, even from DB!
    Name = name;
    Email = email;
}
```

### ✅ Correct Approach: Separate Constructors

```csharp
// GOOD - Private parameterless constructor for EF Core
private Client() { }

// Factory method for new creation
public static Client Create(string name, Email email)
{
    return new Client(name, email);
}

private Client(string name, Email email)
{
    Id = Guid.NewGuid(); // Only for new instances
    Name = name;
    Email = email;
}
```

## 🔄 Entity Lifecycle

```
┌─────────────┐
│  Creation   │  Client.Create(name, email)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Active    │  UpdateName(), UpdateEmail()
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Persistence │  repository.AddAsync(client)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Hydration   │  EF Core loads from DB
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Modification│  UpdateName(), UpdateEmail()
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Deletion   │  repository.DeleteAsync(id)
└─────────────┘
```

## 📝 Summary

### What We Learned

1. ✅ Entities have **unique identity** that persists over time
2. ✅ Use **private setters** to enforce encapsulation
3. ✅ Implement **factory methods** for controlled creation
4. ✅ Keep **business logic inside entities** (Rich Domain Model)
5. ✅ Validate at creation and modification to **maintain invariants**
6. ✅ Use **EF Core configurations** to map entities properly
7. ✅ Entities are **mutable** but changes must go through methods

### Key Takeaways

> "An entity is defined by its identity, not its attributes. Two clients with the same name and email are still different people if they have different IDs."

> "Always maintain invariants. An entity should never be in an invalid state."

> "Encapsulation isn't just private setters—it's about protecting business rules."

## 🔗 Next Steps

- **[Value Objects](./ValueObject.md)**: Learn about immutable objects defined by their values
- **[Aggregates](./Aggregate.md)**: Understand how entities form consistency boundaries
- **[Repositories](./Repository.md)**: Data access patterns for entities

---

**Related Files in Project**:
- `OrderContext.Domain/Client.cs` - Entity implementation
- `OrderContext.Infrastructure/ClientConfiguration.cs` - EF Core configuration
