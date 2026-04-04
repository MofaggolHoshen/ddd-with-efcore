# Repository in Domain-Driven Design

> **Status**: 🚧 Coming Soon  
> **Branch**: `repository-in-ef` (to be created)

## 📖 What is a Repository?

A **Repository** provides an abstraction for accessing and persisting aggregates. It acts like an in-memory collection, hiding the details of data access from the domain layer.

### Key Characteristics

- One repository per aggregate root
- Provides collection-like interface
- Abstracts persistence details
- Returns fully-formed aggregates
- Lives at the boundary between domain and infrastructure

## 🎯 Preview

This section will cover:

- ✨ Repository pattern fundamentals
- ✨ Generic vs specific repositories
- ✨ Implementing with EF Core
- ✨ Unit of Work pattern
- ✨ Query vs command methods

### Example Interface

```csharp
public interface IClientRepository
{
    Task<Client> GetByIdAsync(Guid id);
    Task<List<Client>> GetAllAsync();
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Guid id);
}
```

---

📌 **Check back soon** or switch to the appropriate branch to learn more!
