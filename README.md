# Domain Driven Design with Entity Framework Core (EF)

A practical implementation of Domain-Driven Design (DDD) tactical patterns with Entity Framework Core in .NET 10.

## 🎯 About This Project

This project demonstrates how to build a rich domain model using DDD principles with Entity Framework Core. The implementation focuses on the **Order Context** domain, showcasing how to properly structure entities, value objects, and their persistence using EF Core.

## 📚 DDD Concepts - Step by Step

This project explains DDD concepts incrementally. Each concept is detailed in a separate document:

1. **[Entity](./docs/Entity.md)** ✅ - Objects with unique identity (Client example)
2. **[Value Object](./docs/ValueObject.md)** ✅ - Immutable objects defined by their values (Email example)
3. **[Aggregate](./docs/Aggregate.md)** ✅ - Cluster of objects treated as a unit (Client + Email example)
4. **[Domain Service](./docs/DomainService.md)** ✅ - Business logic that doesn't belong to entities
5. **[Repository](./docs/Repository.md)** - Abstraction for data access
6. **[Domain Event](./docs/DomainEvent.md)** - Significant occurrences in the domain

> **Note**: Concepts are explained as they are implemented in different branches. Currently on branch: `main`

## 🔧 Key Implementation Highlights

### Domain Layer
- **Client Entity**: Aggregate root with encapsulated business logic
- **Email Value Object**: Immutable type with built-in validation
- **Domain Services**: Stateless operations for cross-entity business logic
  - `IEmailUniquenessChecker` - Validates email uniqueness across clients
  - `ClientRegistrationService` - Handles client registration with validation
  - `ClientTransferService` - Manages client email updates
- **DomainException**: Custom exception for domain rule violations
- **Private Setters**: Enforces encapsulation and prevents invalid state
- **Factory Methods**: Ensures objects are always created in valid state
- **Value Conversion**: EF Core mapping for value objects

### Infrastructure Layer
- **EF Core DbContext**: `OrderDbContext` for data persistence
- **Fluent API Configuration**: `ClientConfiguration` for entity mapping
- **Value Object Mapping**: Using `HasConversion` to persist Email as string
- **Domain Service Implementations**: Infrastructure-dependent implementations
  - `EmailUniquenessChecker` - Uses DbContext to check email uniqueness

### Design Patterns Applied
- ✅ Factory Method Pattern
- ✅ Encapsulation
- ✅ Immutability (Value Objects)
- ✅ Aggregate Pattern
- ✅ Domain Service Pattern
- ✅ Dependency Injection
- ✅ Repository Pattern (coming soon)

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 or later
- SQL Server (or modify for your preferred database)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/MofaggolHoshen/domain-driven-design-with-efcore.git
   cd domain-driven-design-with-efcore
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

## 💡 Current Branch: `main`

This branch contains the complete implementation of **Domain-Driven Design** tactical patterns with Entity Framework Core.

### What's Covered:
- Entity and Aggregate Root patterns (Client)
- Value Objects with immutability (Email)
- Domain Services for cross-entity business logic
- Infrastructure layer with EF Core persistence
- Comprehensive unit tests

### Key Files:

#### Domain Layer
- `OrderContext.Domain/Client.cs` - Aggregate Root implementation
- `OrderContext.Domain/Email.cs` - Value Object implementation
- `OrderContext.Domain/Common/ValueObject.cs` - Base class for value objects
- `OrderContext.Domain/Common/DomainException.cs` - Domain rule violation exception
- `OrderContext.Domain/Services/IEmailUniquenessChecker.cs` - Domain service interface
- `OrderContext.Domain/Services/IClientRegistrationService.cs` - Registration service interface
- `OrderContext.Domain/Services/IClientTransferService.cs` - Transfer service interface
- `OrderContext.Domain/Services/ClientRegistrationService.cs` - Domain layer implementation
- `OrderContext.Domain/Services/ClientTransferService.cs` - Domain layer implementation

#### Infrastructure Layer
- `OrderContext.Infrastructure/Services/EmailUniquenessChecker.cs` - Infrastructure implementation
- `OrderContext.Infrastructure/ClientConfiguration.cs` - EF Core configuration
- `OrderContext.Infrastructure/OrderDbContext.cs` - EF Core DbContext

#### Tests
- `OrderContext.Tests/ClientTest.cs` - Client entity tests
- `OrderContext.Tests/EmailTests.cs` - Email value object tests
- `OrderContext.Tests/ClientRegistrationServiceTests.cs` - Registration service tests
- `OrderContext.Tests/ClientTransferServiceTests.cs` - Transfer service tests
- `OrderContext.Tests/ClientConfigurationTests.cs` - EF Core configuration tests

See the documentation for detailed explanations:
- [Entity documentation](./docs/Entity.md)
- [Value Object documentation](./docs/ValueObject.md)
- [Aggregate documentation](./docs/Aggregate.md)
- [Domain Service documentation](./docs/DomainService.md)

## 📖 Learning Path

This is an educational project designed to teach DDD concepts step by step:

1. **Entities** ✅ - Understanding identity and lifecycle
2. **Value Objects** ✅ - Immutability and equality
3. **Aggregates** ✅ - Consistency boundaries and Aggregate Root
4. **Domain Services** ✅ - Cross-entity logic and layer placement
5. **Repositories** - Data access abstraction
6. **Domain Events** - Decoupled communication

Each concept is documented with practical implementation examples.

## 🎓 Best Practices Demonstrated

1. **Encapsulation** - Private setters and fields
2. **Validation** - Always in the domain, not just UI/API
3. **Factory Methods** - Controlled object creation
4. **Immutability** - For value objects
5. **Separation of Concerns** - Domain vs Infrastructure
6. **Rich Domain Model** - Business logic in the domain
7. **Ubiquitous Language** - Code reflects business concepts
8. **Aggregate Design** - Small aggregates, reference by ID
9. **Value Conversion** - EF Core mapping for value objects
10. **Domain Services** - Stateless operations for cross-entity logic
11. **Interface Segregation** - Interfaces in Domain, implementations where needed
12. **Dependency Injection** - Loose coupling between layers

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

---

**Author**: [Mofaggol Hoshen](https://github.com/MofaggolHoshen)  
**Repository**: [domain-driven-design-with-efcore](https://github.com/MofaggolHoshen/domain-driven-design-with-efcore)