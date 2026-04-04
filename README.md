# Domain-Driven Design with Entity Framework Core

A practical implementation of Domain-Driven Design (DDD) tactical patterns with Entity Framework Core in .NET 10.

## 🎯 About This Project

This project demonstrates how to build a rich domain model using DDD principles with Entity Framework Core. The implementation focuses on the **Order Context** domain, showcasing how to properly structure entities, value objects, and their persistence using EF Core.

## 📁 Project Structure

```
DddWithEntityFrameworkCore/
│
├── OrderContext.Domain/           # Domain Layer
│   ├── Client.cs                  # Entity (Aggregate Root)
│   └── Email.cs                   # Value Object
│
├── OrderContext.Infrastructure/   # Infrastructure Layer
│   ├── OrderDbContext.cs          # EF Core DbContext
│   └── ClientConfiguration.cs     # EF Core Entity Configuration
│
└── OrderContext.Tests/            # Unit Tests
```

## 📚 DDD Concepts - Step by Step

This project explains DDD concepts incrementally. Each concept is detailed in a separate document:

1. **[Entity](./docs/Entity.md)** ✅ - Objects with unique identity (Client example)
2. **[Value Object](./docs/ValueObject.md)** - Immutable objects defined by their values (Email example)
3. **[Aggregate](./docs/Aggregate.md)** - Cluster of objects treated as a unit
4. **[Domain Service](./docs/DomainService.md)** - Business logic that doesn't belong to entities
5. **[Repository](./docs/Repository.md)** - Abstraction for data access
6. **[Domain Event](./docs/DomainEvent.md)** - Significant occurrences in the domain

> **Note**: Concepts are explained as they are implemented in different branches. Currently on branch: `entity-in-ef`

## 🔧 Key Implementation Highlights

### Domain Layer
- **Client Entity**: Aggregate root with encapsulated business logic
- **Email Value Object**: Immutable type with built-in validation
- **Private Setters**: Enforces encapsulation and prevents invalid state
- **Factory Methods**: Ensures objects are always created in valid state

### Infrastructure Layer
- **EF Core DbContext**: `OrderDbContext` for data persistence
- **Fluent API Configuration**: `ClientConfiguration` for entity mapping
- **Value Object Mapping**: Using `OwnsOne` to persist Email inline

### Design Patterns Applied
- ✅ Factory Method Pattern
- ✅ Encapsulation
- ✅ Immutability (Value Objects)
- ✅ Aggregate Pattern
- ✅ Repository Pattern (coming soon)

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 or later
- SQL Server (or modify for your preferred database)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/MofaggolHoshen/ddd-with-efcore.git
   cd ddd-with-efcore
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

## 💡 Current Branch: `entity-in-ef`

This branch focuses on implementing **Entities** in Domain-Driven Design with Entity Framework Core.

### What's Covered:
- Creating entities with proper encapsulation
- Using private constructors and factory methods
- Implementing business logic within entities
- Configuring entities in EF Core
- Managing entity lifecycle

### Key Files:
- `OrderContext.Domain/Client.cs` - Entity implementation
- `OrderContext.Infrastructure/ClientConfiguration.cs` - EF Core configuration

See the [Entity documentation](./docs/Entity.md) for detailed explanation.

## 📖 Learning Path

This is an educational project designed to teach DDD concepts step by step:

1. **Start with Entities** (Current) - Understanding identity and lifecycle
2. **Move to Value Objects** - Immutability and equality
3. **Build Aggregates** - Consistency boundaries
4. **Add Domain Services** - Cross-entity logic
5. **Implement Repositories** - Data access abstraction
6. **Introduce Domain Events** - Decoupled communication

Each branch introduces a new concept with practical implementation.

## 🎓 Best Practices Demonstrated

1. **Encapsulation** - Private setters and fields
2. **Validation** - Always in the domain, not just UI/API
3. **Factory Methods** - Controlled object creation
4. **Immutability** - For value objects
5. **Separation of Concerns** - Domain vs Infrastructure
6. **Rich Domain Model** - Business logic in the domain
7. **Ubiquitous Language** - Code reflects business concepts

## 📚 Resources

### Books
- **Domain-Driven Design** by Eric Evans
- **Implementing Domain-Driven Design** by Vaughn Vernon
- **Domain-Driven Design Distilled** by Vaughn Vernon

### Documentation
- [Microsoft - DDD with .NET](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

## 🤝 Contributing

This is an educational project. Feel free to fork and experiment!

## 📄 License

MIT License - See LICENSE file for details

---

**Author**: [Mofaggol Hoshen](https://github.com/MofaggolHoshen)  
**Repository**: [ddd-with-efcore](https://github.com/MofaggolHoshen/ddd-with-efcore)