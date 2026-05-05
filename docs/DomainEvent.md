# Domain Event in Domain-Driven Design

> **Status**: ✅ Implemented  
> **Branch**: `main`

## 📖 What is a Domain Event?

A **Domain Event** represents something significant that happened in the domain. It enables different parts of the system to react to changes without creating tight coupling between aggregates or between the domain and infrastructure.

### Key Characteristics

- Represents a **past occurrence** (named in past tense)
- **Immutable** — properties are set once at construction
- Contains all **relevant context** needed by handlers
- Raised by the aggregate, dispatched **after the transaction commits**

---

## 🏗️ Implementation Overview

```
Domain Layer
├── Common/
│   ├── IDomainEvent.cs            ← Marker interface
│   ├── IDomainEventHandler.cs     ← Generic handler contract
│   ├── IDomainEventDispatcher.cs  ← Dispatcher abstraction
│   └── Entity.cs                  ← Base class that holds events
└── Events/
    ├── ClientRegisteredEvent.cs
    ├── ClientEmailChangedEvent.cs
    └── ClientNameChangedEvent.cs

Infrastructure Layer
├── Services/
│   └── DomainEventDispatcher.cs   ← Resolves handlers from DI
└── OrderDbContext.cs               ← Dispatches events after SaveChanges
```

---

## 🔑 Core Abstractions

### IDomainEvent

```csharp
namespace OrderContext.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
```

### IDomainEventHandler\<TEvent\>

```csharp
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
```

### IDomainEventDispatcher

```csharp
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
```

---

## 🧱 Entity Base Class

All domain entities extend `Entity`, which holds a private collection of domain events:

```csharp
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
```

---

## 📣 Domain Events in This Project

### ClientRegisteredEvent

Raised when a new `Client` is created via `Client.Create()`.

```csharp
public sealed class ClientRegisteredEvent : IDomainEvent
{
    public Guid ClientId { get; }
    public string Name { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }
}
```

### ClientNameChangedEvent

Raised when `client.UpdateName()` is called.

```csharp
public sealed class ClientNameChangedEvent : IDomainEvent
{
    public Guid ClientId { get; }
    public string OldName { get; }
    public string NewName { get; }
    public DateTime OccurredOn { get; }
}
```

### ClientEmailChangedEvent

Raised when `client.UpdateEmail()` is called.

```csharp
public sealed class ClientEmailChangedEvent : IDomainEvent
{
    public Guid ClientId { get; }
    public string OldEmail { get; }
    public string NewEmail { get; }
    public DateTime OccurredOn { get; }
}
```

---

## ⚙️ How Events Are Dispatched

`OrderDbContext.SaveChangesAsync` collects events from all tracked entities, clears them, saves, then dispatches:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entities = ChangeTracker.Entries<Entity>()
        .Where(e => e.Entity.DomainEvents.Count > 0)
        .Select(e => e.Entity)
        .ToList();

    var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

    // Clear before save to prevent re-dispatch on re-entrant saves
    entities.ForEach(e => e.ClearDomainEvents());

    var result = await base.SaveChangesAsync(cancellationToken);

    if (_dispatcher is not null)
        foreach (var domainEvent in domainEvents)
            await _dispatcher.DispatchAsync(domainEvent, cancellationToken);

    return result;
}
```

> Events are dispatched **after** the save succeeds, so handlers can safely read the updated state from the database.

---

## 🔌 Registering a Handler

Register your handler in the DI container and it will be invoked automatically:

```csharp
// Handler implementation
public class SendWelcomeEmailHandler : IDomainEventHandler<ClientRegisteredEvent>
{
    public async Task HandleAsync(ClientRegisteredEvent evt, CancellationToken ct)
    {
        // Send welcome email to evt.Email
    }
}

// Registration
services.AddScoped<IDomainEventHandler<ClientRegisteredEvent>, SendWelcomeEmailHandler>();
```

Multiple handlers for the same event are all invoked in registration order.

---

## 🔄 Event Flow

```
 Client.Create(name, email)
       │
       ├─ Sets Id, Name, Email, CreatedAt
       └─ RaiseDomainEvent(new ClientRegisteredEvent(...))
              │
              ▼
    [Stored in entity._domainEvents]
              │
    UnitOfWork.SaveChangesAsync()
              │
    OrderDbContext.SaveChangesAsync()
              ├─ Collect events from tracked entities
              ├─ Clear events on entities
              ├─ base.SaveChangesAsync()  ← DB write
              └─ IDomainEventDispatcher.DispatchAsync()
                        │
                        ▼
          IDomainEventHandler<ClientRegisteredEvent>
                  .HandleAsync(event)
```

---

## ✅ Tests

Covered in `DomainEventTests.cs`:

| Test | Description |
|------|-------------|
| `Create_Client_RaisesClientRegisteredEvent` | Event is raised on creation |
| `Create_Client_EventOccurredOn_IsUtcNow` | Timestamp is accurate |
| `UpdateName_RaisesClientNameChangedEvent` | Old and new name captured |
| `UpdateEmail_RaisesClientEmailChangedEvent` | Old and new email captured |
| `ClearDomainEvents_RemovesAllEvents` | Collection cleared correctly |
| `MultipleOperations_AccumulateEvents` | Events accumulate in order |
| `SaveChangesAsync_DispatchesDomainEvents_AndClearsThemFromEntity` | Full dispatch flow via DbContext |
| `SaveChangesAsync_WithoutDispatcher_SavesSuccessfully` | Graceful no-op when no dispatcher |
