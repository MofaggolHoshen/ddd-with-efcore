# Domain Driven Design with Entity Framework Core (EF)

A practical implementation of Domain-Driven Design (DDD) tactical patterns with Entity Framework Core in .NET 10.

## 🎯 About This Project

This project demonstrates how to build a rich domain model using DDD principles with Entity Framework Core. The implementation focuses on the **Order Context** domain, showcasing how to properly structure entities, value objects, and their persistence using EF Core.

> **Current Branch**: `repository-in-ef` - Complete implementation of Repository Pattern with Unit of Work

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 or later
- SQL Server (or modify for your preferred database)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/MofaggolHoshen/domain-driven-design-with-efcore.git
cd domain-driven-design-with-efcore

# Restore, build, and test
dotnet restore
dotnet build
dotnet test
```

## 📖 Learning Path

This educational project teaches DDD concepts step by step. Each concept is documented with practical examples:

| # | Concept | Status | Description | Documentation |
|---|---------|--------|-------------|---------------|
| 1 | **Entity** | ✅ | Objects with unique identity | [Entity.md](./docs/Entity.md) |
| 2 | **Value Object** | ✅ | Immutable objects defined by values | [ValueObject.md](./docs/ValueObject.md) |
| 3 | **Aggregate** | ✅ | Cluster of objects as a unit | [Aggregate.md](./docs/Aggregate.md) |
| 4 | **Domain Service** | ✅ | Cross-entity business logic | [DomainService.md](./docs/DomainService.md) |
| 5 | **Repository** | ✅ | Data access abstraction | [Repository.md](./docs/Repository.md) |
| 6 | **Domain Event** | 🔲 | Decoupled communication | [DomainEvent.md](./docs/DomainEvent.md) |

## 🏗️ Project Architecture

This project implements the **three core DDD layers**. The Presentation layer is shown for reference but not included in this project:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                                    │
│            (Not implemented - Web API, MVC, Blazor, Console, etc.)          │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Controllers/           Views/              ViewModels/              │    │
│  │  └── ClientController   └── Client/         └── ClientViewModel      │    │
│  │  Program.cs             └── Shared/         └── CreateClientRequest  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                     │                                        │
│                              Calls  │                                        │
│                                     ▼                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                          APPLICATION LAYER                                   │
│                     OrderContext.Application                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Services/                    DTOs/                                  │    │
│  │  └── ClientApplicationService └── ClientDto                          │    │
│  │                               └── PagedResult<T>                     │    │
│  │  DependencyInjection.cs                                              │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                          │ Uses │                                            │
│                          ▼      ▼                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                           DOMAIN LAYER                                       │
│                      OrderContext.Domain                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Entities/Aggregates     Value Objects      Repositories/            │    │
│  │  └── Client.cs           └── Email.cs       └── IRepository<T,TId>   │    │
│  │                                             └── IClientRepository    │    │
│  │  Services/               Common/            └── IUnitOfWork          │    │
│  │  └── ClientRegistration  └── ValueObject                             │    │
│  │  └── ClientTransfer      └── DomainException                         │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                     ▲                                        │
│                          Implements │                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                        INFRASTRUCTURE LAYER                                  │
│                     OrderContext.Infrastructure                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Repositories/               Services/            Configurations/    │    │
│  │  └── Repository<T,TId>       └── EmailUniqueness  └── ClientConfig   │    │
│  │  └── ClientRepository            Checker                             │    │
│  │  └── UnitOfWork                                                      │    │
│  │  OrderDbContext.cs           DependencyInjection.cs                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                     │                                        │
│                                     ▼                                        │
│                              [ DATABASE ]                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Layer Dependencies

```
┌────────────────────────────┐
│  Presentation Layer        │  ◄─── NOT IMPLEMENTED (Web API, MVC, Blazor, etc.)
│  (UI/API - Optional)       │
└──────────────┬─────────────┘
               │
               │ References
               ▼
┌──────────────────────────┐
│ OrderContext.Application │  ◄─── References Domain only
│   (Orchestration Layer)  │
└──────────────┬───────────┘
               │
               │ References
               ▼
┌──────────────────────────┐
│   OrderContext.Domain    │  ◄─── NO DEPENDENCIES (Pure Domain Logic)
│      (Core Layer)        │
└──────────────┬───────────┘
               │
               │ Implemented by
               ▼
┌────────────────────────────┐
│ OrderContext.Infrastructure │  ◄─── References Domain only
│    (Persistence Layer)      │
└─────────────────────────────┘
```

**Key Principle**: High-level modules (Application) depend on abstractions (Domain interfaces), not on low-level modules (Infrastructure).

## 🔄 Data Flow Example

### Register New Client Flow

```
┌────────────────┐    ┌──────────────────────┐    ┌──────────────┐    ┌────────────────┐
│ Presentation   │───▶│  Application Layer   │───▶│ Domain Layer │───▶│ Infrastructure │
│ (API/UI)       │    │  ClientAppService    │    │              │    │                │
│ Not in project │    └──────────────────────┘    └──────────────┘    └────────────────┘
└────────────────┘              │                        │                     │
Step 1: RegisterClientAsync("John", "john@test.com")   │                     │
                              │                        │                     │
                              ▼                        │                     │
Step 2: Email.Create() ──────────────────────────────▶│ Validates & creates │
                              │                        │ Value Object        │
                              ▼                        │                     │
Step 3: EmailExistsAsync() ──────────────────────────────────────────────────▶
                              │                                              │
                              │◄─────────────────────── Returns: false ──────│
                              │                        │                     │
Step 4: Client.Create() ─────────────────────────────▶│ Creates Aggregate   │
                              │                        │                     │
Step 5: AddAsync(client) ────────────────────────────────────────────────────▶
                              │                                              │
Step 6: SaveChangesAsync() ──────────────────────────────────────────────────▶
                              │                                              │
                              │◄─────────────────────── Persisted to DB ─────│
                              ▼                        │                     │
Step 7: return client.Id ◄────────────────────────────│                     │
```

> **Note**: This project doesn't include a Presentation/UI layer. The Application layer exposes services that can be consumed by any presentation technology (Web API, MVC, Blazor, etc.).

## 🔧 Layer Implementation Details

### Domain Layer
| Component | Purpose |
|-----------|---------|
| `Client.cs` | Aggregate Root with encapsulated business logic |
| `Email.cs` | Value Object with immutability and validation |
| `IRepository<T,TId>` | Generic repository interface |
| `IClientRepository` | Client-specific repository interface |
| `IUnitOfWork` | Transaction management interface |
| `ClientRegistrationService` | Domain service for registration |
| `ClientTransferService` | Domain service for email updates |
| `DomainException` | Custom exception for rule violations |

### Application Layer
| Component | Purpose |
|-----------|---------|
| `ClientApplicationService` | Orchestrates CRUD operations with DTOs |
| `ClientDto` | Read-only data transfer object |
| `PagedResult<T>` | Pagination wrapper |
| `DependencyInjection.cs` | Service registration |

### Infrastructure Layer
| Component | Purpose |
|-----------|---------|
| `Repository<T,TId>` | Generic EF Core repository |
| `ClientRepository` | Client-specific implementation |
| `UnitOfWork` | Transaction coordination |
| `OrderDbContext` | EF Core DbContext |
| `ClientConfiguration` | Fluent API entity mapping |
| `EmailUniquenessChecker` | Infrastructure service implementation |

### Tests (103 tests)
| Test Class | Coverage |
|------------|----------|
| `ClientTest.cs` | Entity behavior |
| `EmailTests.cs` | Value Object validation |
| `ClientRepositoryTests.cs` | Repository operations |
| `UnitOfWorkTests.cs` | Transaction management |
| `ClientApplicationServiceTests.cs` | Application service |
| `ClientRegistrationServiceTests.cs` | Domain service |
| `ClientTransferServiceTests.cs` | Domain service |
| `ClientConfigurationTests.cs` | EF Core mapping |

## ✅ Design Patterns & Best Practices

### Patterns Applied
| Pattern | Implementation |
|---------|---------------|
| Factory Method | `Client.Create()`, `Email.Create()` |
| Repository | `IClientRepository` / `ClientRepository` |
| Unit of Work | `IUnitOfWork` / `UnitOfWork` |
| Aggregate | `Client` as Aggregate Root |
| Value Object | `Email` immutable type |
| Domain Service | `ClientRegistrationService`, `ClientTransferService` |
| Dependency Injection | Interface-based loose coupling |

### Best Practices Demonstrated
1. **Encapsulation** - Private setters and fields
2. **Validation** - Always in the domain, not just UI/API
3. **Factory Methods** - Controlled object creation
4. **Immutability** - For value objects
5. **Separation of Concerns** - Domain vs Application vs Infrastructure
6. **Rich Domain Model** - Business logic in the domain
7. **Ubiquitous Language** - Code reflects business concepts
8. **Aggregate Design** - Small aggregates, reference by ID
9. **Interface Segregation** - Interfaces in Domain, implementations in Infrastructure
10. **DTOs** - Data transfer objects for layer boundaries

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
