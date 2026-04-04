# Value Object in Domain-Driven Design

> **Status**: 🚧 Coming Soon  
> **Branch**: `value-object-in-ef` (to be created)

## 📖 What is a Value Object?

A **Value Object** is an immutable domain object that is defined entirely by its attributes. Unlike entities, value objects have no identity—two value objects with the same values are considered equal.

### Key Characteristics

- No unique identifier
- Immutable (cannot change after creation)
- Equality based on values, not identity
- Replaceable (create new instance instead of modifying)
- Self-validating

## 🎯 Preview

This section will cover:

- ✨ What makes something a value object
- ✨ Implementing immutability in C#
- ✨ Value object equality
- ✨ Mapping value objects with EF Core `OwnsOne`
- ✨ Common value object patterns (Money, Email, Address)

### Example from This Project

```csharp
public class Email
{
    private readonly string _value;
    public string Value => _value;

    private Email(string value)
    {
        _value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty!");
        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format!");

        return new Email(email);
    }
}
```

---

📌 **Check back soon** or switch to the appropriate branch to learn more!
