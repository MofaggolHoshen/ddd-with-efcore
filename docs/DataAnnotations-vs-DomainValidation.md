# Data Annotations vs Domain Validation

> **Best Practice Summary**: ❌ Avoid Data Annotations for domain validation | ✅ Use explicit validation in domain methods

## ⚠️ The Problem with Data Annotations in Domain Entities

Data Annotations (like `[Required]`, `[MaxLength]`, `[EmailAddress]`) are **NOT recommended** for domain validation in DDD for several important reasons:

### 1. Framework Coupling

```csharp
// ❌ BAD - Domain entity coupled to System.ComponentModel.DataAnnotations
using System.ComponentModel.DataAnnotations;

public class Client
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
```

**Problems:**
- ❌ Domain layer depends on infrastructure framework
- ❌ Violates Dependency Inversion Principle
- ❌ Cannot test domain without framework
- ❌ Hard to port to other frameworks

### 2. Cannot Express Complex Business Rules

```csharp
// ❌ Data Annotations can't handle this:
// "Orders under $1000 cannot have discount over 10%"

// ❌ This doesn't work for complex validation
[CustomValidation(typeof(OrderValidator), "ValidateDiscount")]
public decimal Discount { get; set; }

// Need a separate class - domain logic is scattered
public static class OrderValidator
{
    public static ValidationResult ValidateDiscount(decimal discount, ValidationContext context)
    {
        var order = (Order)context.ObjectInstance;
        if (order.TotalAmount < 1000 && discount > 10)
            return new ValidationResult("Invalid discount");
        return ValidationResult.Success;
    }
}
```

**Problems:**
- ❌ Business logic lives outside the entity
- ❌ Complex rules require separate validator classes
- ❌ Anemic domain model
- ❌ Business rules scattered across codebase

### 3. Validation Timing Issues

```csharp
// ❌ Data Annotations are validated AFTER object creation
public class Client
{
    [Required]
    public string Name { get; set; }
}

// Invalid object is created first
var client = new Client(); // Valid object created!
client.Name = null;        // Still valid!

// Validation happens elsewhere (controller, ModelState, etc.)
if (!ModelState.IsValid) { } // Too late! Object already exists in invalid state
```

**Problems:**
- ❌ Object can exist in invalid state
- ❌ Validation is external to the object
- ❌ Depends on external validator to run
- ❌ Can be bypassed

### 4. Wrong Layer of Concern

```csharp
// ❌ BAD - UI/API concerns in domain
public class Client
{
    [Display(Name = "Full Name")]        // UI concern
    [Required(ErrorMessage = "Name is required")]  // UI message
    [StringLength(100, ErrorMessage = "Name too long")]  // UI message
    public string Name { get; set; }
}
```

**Problems:**
- ❌ Mixes presentation concerns with domain
- ❌ Error messages are for end users, not domain concepts
- ❌ Violates separation of concerns

## ✅ Recommended Approach: Explicit Domain Validation

### The DDD Way: Self-Validating Domain Objects

```csharp
// ✅ GOOD - Domain entity with explicit validation
public class Client
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }

    private Client() { } // For EF Core

    // Factory method with validation
    public static Client Create(string name, Email email)
    {
        // EXPLICIT validation - business rules in domain
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return new Client(name, email);
    }

    // Behavior method with validation
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty!");

        Name = newName;
    }
}
```

**Benefits:**
- ✅ Framework-agnostic (pure C#)
- ✅ Business rules in the domain
- ✅ Cannot create invalid objects
- ✅ Explicit and testable
- ✅ Complex rules are easy

## 🎯 When to Use Data Annotations

Data Annotations **ARE acceptable** in specific scenarios:

### ✅ 1. EF Core Mapping Configuration (Limited Use)

```csharp
// ✅ ACCEPTABLE - For database mapping only
using System.ComponentModel.DataAnnotations;

public class Client
{
    [Key]  // Database mapping - tells EF Core this is primary key
    public Guid Id { get; private set; }

    public string Name { get; private set; }
    public Email Email { get; private set; }

    // Domain validation still required!
    public static Client Create(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        return new Client(name, email);
    }
}
```

**Note**: Even better to use **Fluent API** instead:

```csharp
// ✅ BETTER - Use Fluent API in configuration class
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);  // Replaces [Key] attribute

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.OwnsOne<Email>(c => c.Email);
    }
}
```

**Why Fluent API is better:**
- ✅ Keeps domain clean
- ✅ Infrastructure concerns in infrastructure layer
- ✅ More powerful and flexible

### ✅ 2. DTOs and API Request Models

```csharp
// ✅ GOOD - Data Annotations in API layer
namespace OrderContext.API.Models
{
    public class CreateClientRequest  // This is a DTO, NOT a domain entity
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }
}

// Controller usage
[HttpPost]
public IActionResult CreateClient([FromBody] CreateClientRequest request)
{
    // Data annotations validate here for user experience
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Domain validation still happens!
    var email = Email.Create(request.Email);
    var client = Client.Create(request.Name, email);

    return Ok(client);
}
```

**Why this is acceptable:**
- ✅ DTOs are in the API/presentation layer
- ✅ Provides early feedback to users
- ✅ Domain validation still required
- ✅ Clear separation of concerns

### ✅ 3. View Models in MVC

```csharp
// ✅ GOOD - View model for UI binding
public class ClientViewModel
{
    [Display(Name = "Client Name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Display(Name = "Email Address")]
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
```

## 📊 Comparison Table

| Aspect | Data Annotations | Explicit Validation (DDD) |
|--------|------------------|---------------------------|
| **Location** | Attributes on properties | Methods in entity |
| **Framework Dependency** | ❌ Yes (System.ComponentModel) | ✅ No (pure C#) |
| **Complex Rules** | ❌ Difficult | ✅ Easy |
| **Timing** | ❌ External (ModelState) | ✅ At creation/modification |
| **Invalid State** | ❌ Possible | ✅ Impossible |
| **Testability** | ❌ Harder | ✅ Easy |
| **Domain Expression** | ❌ Poor | ✅ Excellent |
| **Use in Domain** | ❌ Not recommended | ✅ Recommended |
| **Use in DTOs/ViewModels** | ✅ Acceptable | ⚠️ Overkill |

## 🏗️ Recommended Architecture

```
┌─────────────────────────────────────────────┐
│  Presentation Layer (API/UI)                │
│  - DTOs with Data Annotations ✅            │
│  - View Models with Data Annotations ✅     │
│  - Early validation for UX                  │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│  Application Layer                          │
│  - Maps DTOs to Domain Objects              │
│  - Orchestrates use cases                   │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│  Domain Layer (NO Data Annotations) ✅      │
│  - Entities with explicit validation        │
│  - Value Objects with explicit validation   │
│  - Business rules in methods                │
│  - Framework-agnostic                       │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│  Infrastructure Layer                       │
│  - EF Core Fluent API Configuration ✅      │
│  - Database constraints                     │
└─────────────────────────────────────────────┘
```

## 💡 Best Practice Example: Your Client Entity

### ❌ Wrong Approach (Data Annotations)

```csharp
// DON'T DO THIS in Domain Layer
using System.ComponentModel.DataAnnotations;

public class Client
{
    [Key]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    // Public setters = can create invalid state
    // No business logic in entity = anemic model
}
```

### ✅ Correct Approach (DDD)

```csharp
// Domain Layer - Clean, no framework dependencies
public class Client
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }  // Value Object
    public DateTime CreatedAt { get; private set; }

    private Client() { }  // EF Core only

    // Factory method with business validation
    public static Client Create(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return new Client(name, email);
    }

    private Client(string name, Email email)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        CreatedAt = DateTime.UtcNow;
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

### ✅ EF Core Configuration (Infrastructure Layer)

```csharp
// Infrastructure Layer - Separate from domain
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);  // Instead of [Key]

        builder.Property(c => c.Name)
            .IsRequired()           // Instead of [Required]
            .HasMaxLength(100);     // Instead of [StringLength]

        builder.OwnsOne<Email>(c => c.Email);
        builder.Navigation(c => c.Email).IsRequired();
    }
}
```

### ✅ API DTO (Presentation Layer)

```csharp
// API Layer - Data Annotations acceptable here
public class CreateClientRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
}

// Controller
[HttpPost]
public IActionResult CreateClient([FromBody] CreateClientRequest request)
{
    // Data Annotations validated here (ModelState)
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Domain validation happens here (cannot be bypassed)
    var email = Email.Create(request.Email);
    var client = Client.Create(request.Name, email);

    _repository.Add(client);
    return Ok(client);
}
```

## 🎓 Alternative: FluentValidation for DTOs

If you want more powerful validation for DTOs, use **FluentValidation** instead of Data Annotations:

```csharp
// Install: dotnet add package FluentValidation.AspNetCore

public class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be 2-100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
```

**Benefits over Data Annotations:**
- ✅ More expressive
- ✅ Better composition
- ✅ Easier to test
- ✅ Cleaner DTOs

## 📝 Summary & Recommendations

### ❌ DON'T Use Data Annotations For:

1. **Domain Entities** - Use explicit validation methods
2. **Business Rules** - Use domain methods
3. **Complex Validation** - Too limited
4. **Invariants** - Need explicit code

### ✅ DO Use Data Annotations For:

1. **DTOs/Request Models** - Quick validation for APIs
2. **View Models** - UI binding and validation
3. **Simple Format Validation** - Email, phone, etc. in presentation layer

### ✅ BEST PRACTICE Use Instead:

1. **Domain Entities**: Explicit validation in methods
2. **EF Core Mapping**: Fluent API configuration
3. **DTOs (optional)**: FluentValidation library

### 🎯 The Golden Rules

1. **Keep your domain clean** - No framework dependencies
2. **Validate explicitly** - Clear business rules in code
3. **Fail fast** - Validate at creation and modification
4. **Separate concerns** - DTOs for API, Entities for domain
5. **Domain is king** - Always validate in domain, even if API validates too

## ✅ Your Current Client Entity

Your current implementation is **almost perfect**:

```csharp
// ✅ Good explicit validation
public static Client Create(string name, Email email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be empty!");

    if (email == null)
        throw new ArgumentNullException(nameof(email));

    return new Client(name, email);
}
```

**Only change needed**: Remove `[Key]` attribute and use Fluent API instead:

```csharp
// Remove this from Client.cs
[Key]  // ← Remove
public Guid Id { get; private set; }

// Add to ClientConfiguration.cs
builder.HasKey(c => c.Id);  // ← Use this instead
```

This keeps your domain layer 100% framework-agnostic! ✨

---

**Related Files**:
- `OrderContext.Domain/Client.cs` - Domain entity (keep clean)
- `OrderContext.Infrastructure/ClientConfiguration.cs` - Use Fluent API here
- Future: `OrderContext.API/Models/CreateClientRequest.cs` - Data Annotations acceptable
