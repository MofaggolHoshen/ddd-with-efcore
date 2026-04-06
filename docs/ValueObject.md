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
var email1 = Email.Create("mofaggol.hoshen@db.com");
var email2 = Email.Create("mofaggol.hoshen@db.com");
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

### The ValueObject Base Class

```csharp
namespace OrderContext.Domain.Common;

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
            .Aggregate(1, (current, obj) =>
            {
                unchecked
                {
                    return current * 23 + (obj?.GetHashCode() ?? 0);
                }
            });
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

### The Email Value Object

```csharp
using OrderContext.Domain.Common;
using System.Text.RegularExpressions;

namespace OrderContext.Domain;

public class Email : ValueObject
{
    // 1. COMPILED REGEX - For efficient email validation
    private static readonly Regex EmailRegex = new Regex(
       @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
       RegexOptions.Compiled | RegexOptions.IgnoreCase
   );

    // 2. IMMUTABLE FIELD - readonly ensures immutability
    private readonly string _value;

    // 3. READ-ONLY PROPERTY - no setter
    public string Value => _value;

    // 4. PRIVATE CONSTRUCTOR - Prevents external instantiation
    private Email(string value) => _value = value;

    // 5. FACTORY METHOD - Only way to create valid Email
    public static Email Create(string email)
    {
        // VALIDATION: Ensures only valid emails exist
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty!");

        // NORMALIZATION: Trim and lowercase
        email = email.Trim().ToLowerInvariant();

        // FORMAT VALIDATION: Using compiled regex
        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException("Invalid email format!");

        // LENGTH VALIDATION: RFC 5321 limit
        if (email.Length > 254)
            throw new ArgumentException("Email exceeds maximum length!");

        return new Email(email);
    }

    // 6. EF CORE MATERIALIZATION - Skips validation for database reads
    public static Email FromDatabase(string value) => new Email(value);

    // 7. VALUE EQUALITY - Inherited from ValueObject base class
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}

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

var email = Email.Create("mofaggol.hoshen@db.com"); // Always valid!
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
var email = new Email { Value = "mofaggol.hoshen@db.com" };
client.Email = email;
email.Value = "hacker@example.com";  // Oops! Client's email changed unexpectedly!

// With immutable value object (GOOD)
var email = Email.Create("mofaggol.hoshen@db.com");
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

### 3. Validation with Regex (Current Implementation)

```csharp
using System.Text.RegularExpressions;
using OrderContext.Domain.Common;

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

    // For EF Core materialization - trusted data, no validation needed
    public static Email FromDatabase(string value) => new Email(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
}
```

## 🗄️ EF Core Mapping with Value Conversion

### The Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderContext.Domain;

namespace OrderContext.Infratructure;

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
                email => email.Value,                    // To database: Email -> string
                value => Email.FromDatabase(value))      // From database: string -> Email
            .HasMaxLength(254)
            .IsRequired();
    }
}
```

### Why `FromDatabase` Instead of `Create`?

```csharp
// ❌ Using Create() - Runs validation on every database read
builder.Property(c => c.Email)
    .HasConversion(
        email => email.Value,
        value => Email.Create(value)  // Validates already-valid data!
    );

// ✅ Using FromDatabase() - Skips validation for trusted data
builder.Property(c => c.Email)
    .HasConversion(
        email => email.Value,
        value => Email.FromDatabase(value)  // Data is already validated
    );
```

**Benefits of `FromDatabase`:**
- ✅ Better performance (no regex validation on reads)
- ✅ Database data is already validated (was validated on write)
- ✅ Avoids exceptions if validation rules change
- ✅ Clear separation of concerns

### Database Schema Result

```sql
CREATE TABLE Clients (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(254) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
)
```

**Benefits:**
- ✅ Email stored as simple string column
- ✅ Value object abstraction in code
- ✅ No separate Email table needed
- ✅ Better performance (no join)

### Alternative: ComplexProperty (EF Core 8+)

For multi-property value objects or when you want EF Core to manage the complex type:

```csharp
// For multi-property value objects like Address
builder.ComplexProperty(c => c.Address, addressBuilder =>
{
    addressBuilder.Property(a => a.Street).HasMaxLength(200).IsRequired();
    addressBuilder.Property(a => a.City).HasMaxLength(100).IsRequired();
    addressBuilder.Property(a => a.ZipCode).HasMaxLength(20).IsRequired();
});
```

**When to use each:**
- **Value Conversion**: Single-property value objects (Email, PhoneNumber)
- **ComplexProperty**: Multi-property value objects (Address, Money)
- **OwnsOne**: When you need navigation properties or separate table option

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
var email = Email.Create("mofaggol.hoshen@db.com");        // Validated ✅
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

var email = new Email { Value = "mofaggol.hoshen@db.com" };
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

### Email Validation Tests

```csharp
public class EmailTests
{
    #region Validation Tests

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("a@b.co")]
    public void Create_WithValidEmail_ReturnsEmailInstance(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);

        // Assert
        Assert.NotNull(email);
        Assert.Equal(validEmail.ToLowerInvariant(), email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_WithNullOrWhitespace_ThrowsArgumentException(string? invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail!));
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("plaintext")]
    [InlineData("@nodomain.com")]
    [InlineData("missing@.com")]
    [InlineData("missing@domain")]
    [InlineData("spaces in@email.com")]
    [InlineData("double@@at.com")]
    public void Create_WithInvalidFormat_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        Assert.Contains("Invalid email format", exception.Message);
    }

    [Fact]
    public void Create_WithEmailExceeding254Characters_ThrowsArgumentException()
    {
        // Arrange
        var longLocalPart = new string('a', 250);
        var tooLongEmail = $"{longLocalPart}@test.com";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(tooLongEmail));
        Assert.Contains("maximum length", exception.Message);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var email = Email.Create("  test@example.com  ");
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Create_ConvertsToLowercase()
    {
        var email = Email.Create("Test.User@EXAMPLE.COM");
        Assert.Equal("test.user@example.com", email.Value);
    }

    #endregion

    #region Value Object Equality Tests

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");
        Assert.NotEqual(email1, email2);
    }

    [Fact]
    public void Equals_WithDifferentCasing_ReturnsTrue()
    {
        // Emails are normalized to lowercase
        var email1 = Email.Create("TEST@example.com");
        var email2 = Email.Create("test@EXAMPLE.com");
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void GetHashCode_SameEmails_ReturnsSameHashCode()
    {
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");
        Assert.Equal(email1.GetHashCode(), email2.GetHashCode());
    }

    #endregion
}
```

### EF Core Configuration Tests

```csharp
public class ClientConfigurationTests
{
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    [Fact]
    public void SaveClient_WithEmail_PersistsEmailValue()
    {
        using var context = CreateDbContext();
        var email = Email.Create("persistence@test.com");
        var client = Client.Create("Test User", email);

        context.Clients.Add(client);
        context.SaveChanges();

        context.ChangeTracker.Clear();
        var savedClient = context.Clients.First();
        Assert.Equal("persistence@test.com", savedClient.Email.Value);
    }

    [Fact]
    public void Email_IsConfiguredWithValueConversion()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var emailProperty = entityType?.FindProperty(nameof(Client.Email));

        Assert.NotNull(emailProperty);
        Assert.NotNull(emailProperty.GetValueConverter());
    }

    [Fact]
    public void EmailProperty_HasMaxLength254()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var emailProperty = entityType?.FindProperty(nameof(Client.Email));

        Assert.NotNull(emailProperty);
        Assert.Equal(254, emailProperty.GetMaxLength());
    }

    [Fact]
    public void ModifyEmail_TracksChanges()
    {
        using var context = CreateDbContext();
        var client = Client.Create("Test User", Email.Create("original@test.com"));
        context.Clients.Add(client);
        context.SaveChanges();

        client.UpdateEmail(Email.Create("modified@test.com"));
        var entry = context.Entry(client);

        Assert.Equal(EntityState.Modified, entry.State);
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

### What We Implemented

1. ✅ **ValueObject Base Class** - Abstract base with equality logic
2. ✅ **Email Value Object** - Immutable, validated, with regex
3. ✅ **Value Conversion** - EF Core mapping with `HasConversion`
4. ✅ **FromDatabase Method** - Optimized materialization without validation
5. ✅ **Comprehensive Tests** - Validation and EF Core configuration tests

### Key Implementation Details

| Feature | Implementation |
|---------|---------------|
| Base Class | `ValueObject` with `GetEqualityComponents()` |
| Validation | Factory method `Create()` with regex |
| EF Core Mapping | Value conversion with `FromDatabase()` |
| Immutability | `readonly` field, no setter |
| Equality | Override via base class |
| Testing | xUnit with InMemory database |

### Key Takeaways

> **"Value Objects are defined by WHAT they are, not WHO they are."**

> **"Use `FromDatabase()` for EF Core materialization to skip redundant validation."**

> **"Value conversion is ideal for single-property value objects like Email."**

## 🔗 Next Steps

- **[Entity](./Entity.md)**: Understand objects with identity
- **[Aggregates](./Aggregate.md)**: How value objects fit in aggregates
- **[Data Annotations vs Domain Validation](./DataAnnotations-vs-DomainValidation.md)**: Validation patterns

---

**Related Files in Project**:
- `OrderContext.Domain/Email.cs` - Value Object implementation
- `OrderContext.Infrastructure/ClientConfiguration.cs` - EF Core `OwnsOne` configuration
- `OrderContext.Domain/Client.cs` - Entity using Email value object
