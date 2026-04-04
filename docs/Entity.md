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

## 🛡️ Validation Best Practices for Entities

### The Golden Rule: Always Valid State

> **"An entity should never exist in an invalid state"**

This is the fundamental principle of domain-driven validation. If an entity exists in memory, it must be valid according to business rules.

### 1. Validate at Creation (Factory Method)

**✅ Best Practice**: Validate all invariants when creating the entity

```csharp
public static Client Create(string name, Email email)
{
    // Validation BEFORE creating the object
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be empty!");

    if (email == null)
        throw new ArgumentNullException(nameof(email));

    // Only create if all validations pass
    return new Client(name, email);
}
```

**Why this works:**
- ✅ No way to create an invalid entity
- ✅ All business rules enforced at construction
- ✅ Fail fast—errors detected immediately
- ✅ Clear error messages for each violation

**❌ Anti-Pattern: Post-Construction Validation**

```csharp
// BAD - Object exists before validation
public static Client Create(string name, Email email)
{
    var client = new Client(name, email); // Created invalid object!

    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be empty!");

    return client; // Too late, already created
}
```

### 2. Validate at Modification (Behavior Methods)

**✅ Best Practice**: Validate before changing state

```csharp
public void UpdateName(string newName)
{
    // Validate BEFORE changing state
    if (string.IsNullOrWhiteSpace(newName))
        throw new ArgumentException("Name cannot be empty!");

    // Only change if validation passes
    Name = newName;
}

public void UpdateEmail(Email newEmail)
{
    // Validate BEFORE changing state
    if (newEmail == null)
        throw new ArgumentNullException(nameof(newEmail));

    // Only change if validation passes
    Email = newEmail;
}
```

**Why this works:**
- ✅ Entity remains valid if validation fails
- ✅ Transaction-safe (can roll back if needed)
- ✅ Atomic operation (all-or-nothing)

**❌ Anti-Pattern: Change First, Validate Later**

```csharp
// BAD - State changed before validation
public void UpdateName(string newName)
{
    Name = newName; // Already changed!

    if (string.IsNullOrWhiteSpace(Name))
    {
        // Now what? Entity is in invalid state
        throw new ArgumentException("Name cannot be empty!");
    }
}
```

### 3. Validate Invariants (Business Rules)

**What are Invariants?**

Invariants are business rules that must ALWAYS be true for an entity.

**Examples in Client Entity:**

```csharp
// INVARIANT 1: Client must always have a name
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Name cannot be empty!");

// INVARIANT 2: Client must always have a valid email
if (email == null)
    throw new ArgumentNullException(nameof(email));

// INVARIANT 3: Email must always be valid (enforced by Email value object)
// The Email.Create() method ensures this
```

**More Complex Invariants Example:**

```csharp
public class Order
{
    public List<OrderItem> Items { get; private set; }

    public void AddItem(OrderItem item)
    {
        // INVARIANT: Order cannot exceed 100 items
        if (Items.Count >= 100)
            throw new InvalidOperationException("Order cannot have more than 100 items!");

        // INVARIANT: Cannot add duplicate items
        if (Items.Any(i => i.ProductId == item.ProductId))
            throw new InvalidOperationException("Item already exists in order!");

        Items.Add(item);
    }
}
```

### 4. Where to Validate

#### ✅ Domain Layer (Entity) - ALWAYS

```csharp
// In Entity - REQUIRED validation
public static Client Create(string name, Email email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be empty!");

    return new Client(name, email);
}
```

**Why:**
- Last line of defense
- Protects domain integrity
- Independent of UI/API
- Cannot be bypassed

#### ✅ Application Layer (API/UI) - User Experience

```csharp
// In API Controller - OPTIONAL validation for better UX
[HttpPost]
public IActionResult CreateClient([FromBody] CreateClientRequest request)
{
    // Early validation for better error messages
    if (string.IsNullOrWhiteSpace(request.Name))
        return BadRequest("Name is required");

    // Domain validation still happens here
    var email = Email.Create(request.Email);
    var client = Client.Create(request.Name, email);

    return Ok(client);
}
```

**Why:**
- Better user experience
- Faster feedback
- Clearer error messages
- But NOT a replacement for domain validation!

#### ❌ Database Layer - NEVER as Primary Validation

```sql
-- BAD - Don't rely on database constraints alone
CREATE TABLE Clients (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL -- DB enforces, but too late!
)
```

**Why not:**
- Error happens too late (after all domain logic)
- Generic database errors (not business-friendly)
- Domain objects don't reflect business rules
- Hard to test

### 5. Validation Strategies

#### Strategy A: Fail Fast (Throw Exceptions)

**✅ Use when**: Validation failure is exceptional and should stop execution

```csharp
public void UpdateName(string newName)
{
    if (string.IsNullOrWhiteSpace(newName))
        throw new ArgumentException("Name cannot be empty!"); // Fail Fast

    Name = newName;
}
```

**Pros:**
- Simple and clear
- Stops invalid operations immediately
- Easy to implement

**Cons:**
- Cannot collect multiple validation errors
- Exceptions are expensive

#### Strategy B: Return Validation Result

**✅ Use when**: You want to collect all validation errors

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public ValidationResult Validate()
{
    var result = new ValidationResult { IsValid = true };

    if (string.IsNullOrWhiteSpace(Name))
    {
        result.IsValid = false;
        result.Errors.Add("Name cannot be empty");
    }

    if (Email == null)
    {
        result.IsValid = false;
        result.Errors.Add("Email is required");
    }

    return result;
}
```

**Pros:**
- Collect all errors at once
- Better user experience (show all issues)
- No exception overhead

**Cons:**
- More complex
- Can still create invalid objects if not careful

#### Strategy C: Hybrid Approach (Recommended)

```csharp
// Validate at creation - throw exception (fail fast)
public static Client Create(string name, Email email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be empty!");

    if (email == null)
        throw new ArgumentNullException(nameof(email));

    return new Client(name, email);
}

// Validate at modification - throw exception (fail fast)
public void UpdateName(string newName)
{
    if (string.IsNullOrWhiteSpace(newName))
        throw new ArgumentException("Name cannot be empty!");

    Name = newName;
}

// Optional: Provide a validation method for pre-checking
public static ValidationResult CanCreate(string name, Email email)
{
    var result = new ValidationResult { IsValid = true };

    if (string.IsNullOrWhiteSpace(name))
    {
        result.IsValid = false;
        result.Errors.Add("Name cannot be empty");
    }

    if (email == null)
    {
        result.IsValid = false;
        result.Errors.Add("Email is required");
    }

    return result;
}
```

### 6. Validation Layering

```
┌──────────────────────────────────────────┐
│  UI/API Layer (Optional)                 │
│  - Input validation                      │
│  - Format validation                     │
│  - Early user feedback                   │
└──────────────┬───────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────┐
│  Application Layer (Optional)            │
│  - DTOs validation                       │
│  - FluentValidation, DataAnnotations     │
│  - Cross-field validation                │
└──────────────┬───────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────┐
│  Domain Layer (REQUIRED) ✅              │
│  - Business rules (invariants)           │
│  - Entity validation                     │
│  - Value object validation               │
│  - CANNOT BE BYPASSED                    │
└──────────────┬───────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────┐
│  Database Layer (Safety Net)             │
│  - NOT NULL constraints                  │
│  - Unique constraints                    │
│  - Foreign keys                          │
└──────────────────────────────────────────┘
```

### 7. Common Validation Patterns

#### Pattern 1: Null/Empty Validation

```csharp
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Name cannot be empty!");

if (email == null)
    throw new ArgumentNullException(nameof(email));
```

#### Pattern 2: Range Validation

```csharp
if (age < 0 || age > 150)
    throw new ArgumentOutOfRangeException(nameof(age), "Age must be between 0 and 150");
```

#### Pattern 3: Format Validation (Delegate to Value Object)

```csharp
// In Entity
public Email Email { get; private set; }

// In Email Value Object
public static Email Create(string email)
{
    if (!email.Contains('@'))
        throw new ArgumentException("Invalid email format!");

    return new Email(email);
}
```

#### Pattern 4: Business Rule Validation

```csharp
public void ApplyDiscount(decimal discountPercentage)
{
    if (discountPercentage < 0 || discountPercentage > 100)
        throw new ArgumentException("Discount must be between 0 and 100 percent");

    if (TotalAmount < 1000 && discountPercentage > 10)
        throw new InvalidOperationException("Orders under $1000 cannot have discount over 10%");

    // Apply discount
}
```

#### Pattern 5: State Validation

```csharp
public void Ship()
{
    if (Status != OrderStatus.Paid)
        throw new InvalidOperationException("Cannot ship order that is not paid");

    if (!Items.Any())
        throw new InvalidOperationException("Cannot ship order with no items");

    Status = OrderStatus.Shipped;
}
```

### 8. Validation Error Handling

#### ✅ Use Specific Exceptions

```csharp
// GOOD - Clear exception types
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Name cannot be empty!", nameof(name));

if (email == null)
    throw new ArgumentNullException(nameof(email));

if (age < 0)
    throw new ArgumentOutOfRangeException(nameof(age), "Age cannot be negative");
```

#### ✅ Include Parameter Name

```csharp
throw new ArgumentException("Name cannot be empty!", nameof(name));
//                                                    ↑
//                                    Parameter name for debugging
```

#### ✅ Provide Clear Messages

```csharp
// GOOD - Clear, actionable message
throw new ArgumentException("Email cannot be empty!");

// BAD - Vague message
throw new ArgumentException("Invalid input");
```

#### ✅ Custom Domain Exceptions (Advanced)

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class InvalidClientNameException : DomainException
{
    public InvalidClientNameException(string name) 
        : base($"Client name '{name}' is invalid. Name cannot be empty or whitespace.") { }
}

// Usage
public static Client Create(string name, Email email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new InvalidClientNameException(name);

    return new Client(name, email);
}
```

### 9. Real-World Example: Client Entity Validation

Here's how validation is implemented in the Client entity:

```csharp
public class Client
{
    // STEP 1: Validate at Creation
    public static Client Create(string name, Email email)
    {
        // INVARIANT 1: Name must not be empty
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        // INVARIANT 2: Email must exist
        if (email == null)
            throw new ArgumentNullException(nameof(email));

        // Email format validation is delegated to Email value object
        // Email.Create() already validated the format

        return new Client(name, email);
    }

    // STEP 2: Validate at Modification
    public void UpdateName(string newName)
    {
        // Re-validate the invariant
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty!");

        Name = newName;
    }

    public void UpdateEmail(Email newEmail)
    {
        // Re-validate the invariant
        if (newEmail == null)
            throw new ArgumentNullException(nameof(newEmail));

        Email = newEmail;
    }
}
```

### 10. Testing Validation

**Always test your validation logic!**

```csharp
[Fact]
public void Create_WithEmptyName_ThrowsArgumentException()
{
    // Arrange
    var email = Email.Create("test@example.com");

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(() => 
        Client.Create("", email));

    Assert.Equal("Name cannot be empty!", exception.Message);
}

[Fact]
public void UpdateName_WithEmptyName_ThrowsArgumentException()
{
    // Arrange
    var email = Email.Create("test@example.com");
    var client = Client.Create("John Doe", email);

    // Act & Assert
    Assert.Throws<ArgumentException>(() => 
        client.UpdateName(""));
}

[Fact]
public void Create_WithValidData_ReturnsClient()
{
    // Arrange
    var email = Email.Create("test@example.com");

    // Act
    var client = Client.Create("John Doe", email);

    // Assert
    Assert.NotNull(client);
    Assert.Equal("John Doe", client.Name);
}
```

## ✅ Validation Best Practices Summary

1. **✅ Validate at the domain layer** - Always, no exceptions
2. **✅ Fail fast** - Throw exceptions immediately on invalid input
3. **✅ Validate before changing state** - Never create invalid objects
4. **✅ Use clear error messages** - Help developers understand what went wrong
5. **✅ Delegate format validation** - Use value objects for complex formats
6. **✅ Test all validation rules** - Every invariant should have a test
7. **✅ Validation in layers is OK** - UI/API can validate too, but domain MUST validate
8. **❌ Never skip domain validation** - It's not optional
9. **❌ Don't rely on database constraints alone** - Domain should be self-validating
10. **❌ Don't change state before validation** - Maintain atomicity

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

## 📚 Related Topics

- **[Data Annotations vs Domain Validation](./DataAnnotations-vs-DomainValidation.md)**: Should you use Data Annotations in entities? (Spoiler: No!)

---

**Related Files in Project**:
- `OrderContext.Domain/Client.cs` - Entity implementation
- `OrderContext.Infrastructure/ClientConfiguration.cs` - EF Core configuration
