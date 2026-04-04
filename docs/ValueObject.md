# Value Object in Domain-Driven Design

> **Branch**: `value-object-in-ef`  
> **Status**: ✅ Implemented

## 📖 What is a Value Object?

A **Value Object** is an immutable domain object that is defined entirely by its attributes or values. Unlike entities, value objects have **no identity**—two value objects with the same values are considered equal and interchangeable.

### The Core Principle

> **"Value Objects are defined by WHAT they are, not WHO they are"**

A $100 bill in your wallet and a $100 bill in mine are the same value—they're interchangeable. That's a Value Object. But your driver's license and mine are different even if they have the same information—those are Entities.

## 🎯 Key Characteristics

### 1. No Identity
```csharp
// Two emails with same value are identical
var email1 = Email.Create("john@example.com");
var email2 = Email.Create("john@example.com");
// email1 equals email2 ✅
```

### 2. Immutable
```csharp
public class Email
{
    private readonly string _value;  // readonly = immutable
    public string Value => _value;   // No setter = cannot change

    // Cannot change email - must create new one
    // email.Value = "new@example.com"; ❌ Won't compile
}
```

### 3. Value-Based Equality
```csharp
// Same value = equal objects
var money1 = Money.Create(100, "USD");
var money2 = Money.Create(100, "USD");
Assert.Equal(money1, money2); // True ✅
```

### 4. Replaceable
```csharp
// Instead of modifying, create new instance
public void UpdateEmail(Email newEmail)
{
    Email = newEmail;  // Replace old value with new
}
```

### 5. Self-Validating
```csharp
// Validates its own format and rules
public static Email Create(string email)
{
    if (!email.Contains('@'))
        throw new ArgumentException("Invalid email format!");

    return new Email(email);
}
```

## 💻 Implementation in This Project

### The Email Value Object

```csharp
namespace OrderContext.Domain;

public class Email
{
    // 1. IMMUTABLE FIELD - readonly ensures immutability
    private readonly string _value;

    // 2. READ-ONLY PROPERTY - no setter
    public string Value => _value;

    // 3. PARAMETERLESS CONSTRUCTOR - For EF Core only
    private Email()
    {
        _value = null!;  // Required for EF Core deserialization
    }

    // 4. PRIVATE CONSTRUCTOR - Prevents external instantiation
    private Email(string value)
    {
        _value = value;
    }

    // 5. FACTORY METHOD - Only way to create valid Email
    public static Email Create(string email)
    {
        // VALIDATION: Ensures only valid emails exist
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty!");

        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format!");

        return new Email(email);
    }
}
```

## 🏗️ Design Patterns Applied

### 1. Factory Method Pattern

**Why**: Ensures only valid value objects are created

```csharp
// ❌ BAD - Public constructor allows invalid creation
public Email(string value)
{
    _value = value;
}

var email = new Email("not-an-email"); // Invalid!

// ✅ GOOD - Factory method enforces validation
private Email(string value) { _value = value; }

public static Email Create(string email)
{
    if (!email.Contains('@'))
        throw new ArgumentException("Invalid email format!");

    return new Email(email);
}

var email = Email.Create("john@example.com"); // Always valid!
```

### 2. Immutability Pattern

**Why**: Value objects should never change once created

```csharp
// ✅ GOOD - Immutable value object
public class Email
{
    private readonly string _value;  // readonly
    public string Value => _value;   // No setter

    private Email(string value)
    {
        _value = value;
    }
}

// ❌ BAD - Mutable value object
public class Email
{
    public string Value { get; set; }  // Can be changed!
}
```

**Why immutability matters:**
```csharp
// With mutable value object (BAD)
var email = new Email { Value = "john@example.com" };
client.Email = email;
email.Value = "hacker@example.com";  // Oops! Client's email changed unexpectedly!

// With immutable value object (GOOD)
var email = Email.Create("john@example.com");
client.Email = email;
// email.Value = "hacker@example.com";  ❌ Won't compile - immutable!
```

### 3. Value Object Base Class (Advanced)

For multiple value objects, create a base class:

```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
```

**Usage:**

```csharp
public class Email : ValueObject
{
    private readonly string _value;
    public string Value => _value;

    private Email(string value) => _value = value;

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty!");
        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format!");

        return new Email(email);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
```

## 🛡️ Validation Best Practices

### 1. Validate in Factory Method (ALWAYS)

```csharp
public static Email Create(string email)
{
    // Validate BEFORE creating object
    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email cannot be empty!");

    if (!email.Contains('@'))
        throw new ArgumentException("Invalid email format!");

    // Could add more validation
    if (email.Length > 254)
        throw new ArgumentException("Email too long!");

    if (!email.Contains('.'))
        throw new ArgumentException("Email must have domain!");

    return new Email(email);
}
```

**Why this works:**
- ✅ Invalid value objects cannot exist
- ✅ Validation logic centralized
- ✅ Type safety throughout application
- ✅ No need to validate elsewhere

### 2. Comprehensive Validation Example

```csharp
public static Email Create(string email)
{
    // NULL/EMPTY CHECK
    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email cannot be empty!");

    // TRIM WHITESPACE
    email = email.Trim().ToLowerInvariant();

    // FORMAT VALIDATION
    if (!email.Contains('@'))
        throw new ArgumentException("Email must contain @ symbol!");

    // STRUCTURE VALIDATION
    var parts = email.Split('@');
    if (parts.Length != 2)
        throw new ArgumentException("Email format invalid!");

    if (string.IsNullOrWhiteSpace(parts[0]))
        throw new ArgumentException("Email local part cannot be empty!");

    if (string.IsNullOrWhiteSpace(parts[1]))
        throw new ArgumentException("Email domain cannot be empty!");

    if (!parts[1].Contains('.'))
        throw new ArgumentException("Email domain must contain a dot!");

    // LENGTH VALIDATION
    if (email.Length > 254)
        throw new ArgumentException("Email exceeds maximum length!");

    // BUSINESS RULES
    if (parts[1].Equals("tempmail.com"))
        throw new ArgumentException("Temporary email addresses not allowed!");

    return new Email(email);
}
```

### 3. Validation with Regex (Advanced)

```csharp
using System.Text.RegularExpressions;

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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
```

## 🗄️ EF Core Mapping with `OwnsOne`

### The Configuration

```csharp
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        // OwnsOne: Email is owned by Client (no separate table)
        builder.OwnsOne<Email>(c => c.Email, email =>
        {
            // Map the private field
            email.Property(e => e.Value)
                .HasColumnName("Email")  // Column name in Client table
                .HasMaxLength(254)       // Database constraint
                .IsRequired();           // NOT NULL constraint
        });

        // Navigation is required
        builder.Navigation(c => c.Email).IsRequired();
    }
}
```

### Database Schema Result

```sql
CREATE TABLE Clients (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(100),
    Email NVARCHAR(254) NOT NULL,  -- Email value inline
    CreatedAt DATETIME2
)
```

**Benefits:**
- ✅ No separate Email table
- ✅ Value object stored inline
- ✅ Better performance (no join)
- ✅ Reflects domain model accurately

### Alternative: Value Conversion (EF Core 2.1+)

```csharp
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        // Value Conversion: Convert Email to/from string
        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,              // To database
                value => Email.Create(value)       // From database
            )
            .HasColumnName("Email")
            .HasMaxLength(254)
            .IsRequired();
    }
}
```

**When to use each:**
- **OwnsOne**: Multi-property value objects (Address, Money)
- **Value Conversion**: Single-property value objects (Email, PhoneNumber)

## 🎨 Common Value Object Examples

### 1. Money Value Object

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { }  // EF Core

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative!");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required!");

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be 3-letter ISO code!");

        return new Money(amount, currency.ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies!");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract different currencies!");

        return new Money(Amount - other.Amount, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

**EF Core Configuration:**
```csharp
builder.OwnsOne(o => o.TotalPrice, money =>
{
    money.Property(m => m.Amount).HasColumnName("TotalAmount");
    money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
});
```

### 2. Address Value Object

```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }

    private Address() { }  // EF Core

    private Address(string street, string city, string state, string zipCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public static Address Create(string street, string city, string state, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required!");
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required!");
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("ZipCode is required!");
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required!");

        return new Address(street, city, state, zipCode, country);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }
}
```

### 3. PhoneNumber Value Object

```csharp
public class PhoneNumber : ValueObject
{
    private readonly string _value;
    public string Value => _value;

    private PhoneNumber() => _value = null!;

    private PhoneNumber(string value) => _value = value;

    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty!");

        // Remove common formatting
        var cleaned = phoneNumber.Replace("-", "")
                                .Replace("(", "")
                                .Replace(")", "")
                                .Replace(" ", "");

        if (!cleaned.All(char.IsDigit))
            throw new ArgumentException("Phone number must contain only digits!");

        if (cleaned.Length < 10 || cleaned.Length > 15)
            throw new ArgumentException("Phone number length invalid!");

        return new PhoneNumber(cleaned);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
```

## ⚖️ Value Object vs Primitive Obsession

### ❌ Primitive Obsession (Anti-Pattern)

```csharp
public class Client
{
    public string Email { get; set; }  // Just a string!
    public decimal AccountBalance { get; set; }  // Just a number!
    public string PhoneNumber { get; set; }  // Just a string!
}

// Problems:
var client = new Client();
client.Email = "not-an-email";  // Invalid!
client.AccountBalance = -1000;  // Negative balance!
client.PhoneNumber = "abc";     // Not a phone number!

// No type safety:
client.Email = client.PhoneNumber;  // Both are strings - compiles!
```

### ✅ Value Objects (Proper DDD)

```csharp
public class Client
{
    public Email Email { get; private set; }
    public Money AccountBalance { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }

    public static Client Create(Email email, Money balance, PhoneNumber phone)
    {
        // All parameters are already validated!
        return new Client(email, balance, phone);
    }
}

// Benefits:
var email = Email.Create("john@example.com");        // Validated ✅
var balance = Money.Create(1000, "USD");             // Validated ✅
var phone = PhoneNumber.Create("555-1234");          // Validated ✅

var client = Client.Create(email, balance, phone);   // Type-safe ✅

// client.Email = client.PhoneNumber;  ❌ Won't compile - different types!
```

**Benefits:**
- ✅ Type safety
- ✅ Validation centralized
- ✅ Domain concepts explicit
- ✅ Cannot mix up values
- ✅ Easier to refactor
- ✅ Self-documenting code

## ❌ Common Mistakes to Avoid

### Mistake 1: Mutable Value Objects

```csharp
// ❌ BAD - Mutable value object
public class Email
{
    public string Value { get; set; }  // Setter!
}

var email = new Email { Value = "john@example.com" };
email.Value = "changed@example.com";  // Changed! Not immutable!
```

**Fix:**
```csharp
// ✅ GOOD - Immutable
public class Email
{
    private readonly string _value;
    public string Value => _value;  // No setter
}
```

### Mistake 2: Public Constructors

```csharp
// ❌ BAD - Public constructor
public Email(string value)
{
    _value = value;
}

var email = new Email("not-valid");  // Can create invalid email!
```

**Fix:**
```csharp
// ✅ GOOD - Private constructor + factory
private Email(string value) => _value = value;

public static Email Create(string email)
{
    // Validation here
    return new Email(email);
}
```

### Mistake 3: Adding Identity

```csharp
// ❌ BAD - Value object with ID
public class Email
{
    public int Id { get; set; }  // NO! This makes it an entity!
    public string Value { get; set; }
}
```

**Fix:**
```csharp
// ✅ GOOD - No identity
public class Email
{
    public string Value { get; }  // Only the value, no ID
}
```

## 🧪 Testing Value Objects

```csharp
public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ReturnsEmail()
    {
        // Arrange & Act
        var email = Email.Create("john@example.com");

        // Assert
        Assert.NotNull(email);
        Assert.Equal("john@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@nodomain")]
    public void Create_WithInvalidFormat_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        Assert.Contains("Invalid email format", exception.Message);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("john@example.com");
        var email2 = Email.Create("john@example.com");

        // Act & Assert
        Assert.Equal(email1, email2);
    }
}
```

## ✅ Value Object Best Practices Summary

1. **✅ Always immutable** - Use `readonly` fields and no setters
2. **✅ Private constructors** - Force use of factory methods
3. **✅ Factory method validation** - Validate before creation
4. **✅ No identity** - No ID property
5. **✅ Value-based equality** - Override `Equals()` and `GetHashCode()`
6. **✅ Use for domain concepts** - Email, Money, Address, not just strings
7. **✅ Map with `OwnsOne`** - Keep inline with owner entity
8. **✅ Self-validating** - Validation logic inside value object
9. **✅ Replace, don't modify** - Create new instances instead of changing
10. **✅ Use records (C# 9+)** - Simplifies implementation

### ❌ Anti-Patterns to Avoid

1. **❌ Mutable value objects** - Defeats the purpose
2. **❌ Public constructors** - Allows invalid creation
3. **❌ Adding identity (ID)** - Makes it an entity
4. **❌ Separate database tables** - Use `OwnsOne` instead
5. **❌ External validation** - Keep validation in value object
6. **❌ Using primitives everywhere** - Primitive obsession
7. **❌ Reference equality** - Must use value equality

## 📝 Summary

### What We Learned

1. ✅ Value Objects are **defined by their values**, not identity
2. ✅ They must be **immutable** (cannot change after creation)
3. ✅ Use **factory methods** for creation with validation
4. ✅ Implement **value-based equality** (or use `record`)
5. ✅ Map with **`OwnsOne`** in EF Core (inline storage)
6. ✅ Replace **primitive obsession** with value objects
7. ✅ Value objects are **self-validating** and type-safe
8. ✅ Use for domain concepts: **Email, Money, Address, PhoneNumber**

### Key Takeaways

> **"If something is defined by what it is (its value) rather than who it is (identity), it's a Value Object."**

> **"Immutability is not optional for Value Objects—it's fundamental to their nature."**

> **"Value Objects fight primitive obsession and make your domain model richer and safer."**

## 🔗 Next Steps

- **[Entity](./Entity.md)**: Understand objects with identity
- **[Aggregates](./Aggregate.md)**: How value objects fit in aggregates
- **[Data Annotations vs Domain Validation](./DataAnnotations-vs-DomainValidation.md)**: Validation patterns

---

**Related Files in Project**:
- `OrderContext.Domain/Email.cs` - Value Object implementation
- `OrderContext.Infrastructure/ClientConfiguration.cs` - EF Core `OwnsOne` configuration
- `OrderContext.Domain/Client.cs` - Entity using Email value object
