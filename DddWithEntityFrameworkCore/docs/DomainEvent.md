# Domain Event in Domain-Driven Design

> **Status**: 🚧 Coming Soon  
> **Branch**: `domain-event-in-ef` (to be created)

## 📖 What is a Domain Event?

A **Domain Event** represents something significant that happened in the domain. It enables different parts of the system to react to changes without creating tight coupling between aggregates.

### Key Characteristics

- Represents a past occurrence
- Immutable
- Named in past tense
- Contains relevant context
- Published and consumed asynchronously

## 🎯 Preview

This section will cover:

- ✨ When to use domain events
- ✨ Event naming conventions
- ✨ Publishing domain events
- ✨ Event handlers
- ✨ Integration with MediatR or other event buses

### Example

```csharp
public class ClientEmailChangedEvent
{
    public Guid ClientId { get; set; }
    public string OldEmail { get; set; }
    public string NewEmail { get; set; }
    public DateTime OccurredOn { get; set; }
}
```

---

📌 **Check back soon** or switch to the appropriate branch to learn more!
