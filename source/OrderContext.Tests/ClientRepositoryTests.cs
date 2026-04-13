using Microsoft.EntityFrameworkCore;
using OrderContext.Domain;
using OrderContext.Infratructure;
using OrderContext.Infratructure.Repositories;

namespace OrderContext.Tests;

/// <summary>
/// Unit tests for ClientRepository implementation.
/// Tests repository operations using in-memory database.
/// </summary>
public class ClientRepositoryTests
{
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingClient_ReturnsClient()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var email = Email.Create("test@example.com");
        var client = Client.Create("Test Client", email);
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(client.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(client.Id, result.Id);
        Assert.Equal("Test Client", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingClient_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsClient()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var email = Email.Create("find@example.com");
        var client = Client.Create("Find Client", email);
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("find@example.com", result.Email.Value);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var email = Email.Create("notfound@example.com");

        // Act
        var result = await repository.GetByEmailAsync(email);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var email = Email.Create("exists@example.com");
        var client = Client.Create("Exists Client", email);
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        // Act
        var exists = await repository.EmailExistsAsync(email);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var email = Email.Create("notexists@example.com");

        // Act
        var exists = await repository.EmailExistsAsync(email);

        // Assert
        Assert.False(exists);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithClients_ReturnsAllClients()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var clients = new[]
        {
            Client.Create("Client 1", Email.Create("client1@example.com")),
            Client.Create("Client 2", Email.Create("client2@example.com")),
            Client.Create("Client 3", Email.Create("client3@example.com"))
        };
        await context.Clients.AddRangeAsync(clients);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidClient_ClientIsPersisted()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var email = Email.Create("new@example.com");
        var client = Client.Create("New Client", email);

        // Act
        await repository.AddAsync(client);
        await context.SaveChangesAsync();

        // Assert
        var persisted = await context.Clients.FindAsync(client.Id);
        Assert.NotNull(persisted);
        Assert.Equal("New Client", persisted.Name);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ExistingClient_ClientIsDeleted()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);
        var email = Email.Create("delete@example.com");
        var client = Client.Create("Delete Me", email);
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        // Act
        repository.Remove(client);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.Clients.FindAsync(client.Id);
        Assert.Null(deleted);
    }

    #endregion

    #region GetClientsCreatedBetweenAsync Tests

    [Fact]
    public async Task GetClientsCreatedBetweenAsync_WithMatchingClients_ReturnsFilteredClients()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        var client1 = Client.Create("Client 1", Email.Create("c1@example.com"));
        var client2 = Client.Create("Client 2", Email.Create("c2@example.com"));

        await context.Clients.AddRangeAsync(client1, client2);
        await context.SaveChangesAsync();

        var startDate = DateTime.UtcNow.AddMinutes(-1);
        var endDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var result = await repository.GetClientsCreatedBetweenAsync(startDate, endDate);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        for (int i = 1; i <= 10; i++)
        {
            var client = Client.Create($"Client {i:D2}", Email.Create($"client{i}@example.com"));
            await context.Clients.AddAsync(client);
        }
        await context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(page: 1, pageSize: 3);

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal(10, totalCount);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsCorrectItems()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        for (int i = 1; i <= 10; i++)
        {
            var client = Client.Create($"Client {i:D2}", Email.Create($"client{i}@example.com"));
            await context.Clients.AddAsync(client);
        }
        await context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(page: 2, pageSize: 3);

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal(10, totalCount);
    }

    [Fact]
    public async Task GetPagedAsync_LastPartialPage_ReturnsRemainingItems()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        for (int i = 1; i <= 10; i++)
        {
            var client = Client.Create($"Client {i:D2}", Email.Create($"client{i}@example.com"));
            await context.Clients.AddAsync(client);
        }
        await context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(page: 4, pageSize: 3);

        // Assert
        Assert.Single(items);
        Assert.Equal(10, totalCount);
    }

    [Fact]
    public async Task GetPagedAsync_EmptyRepository_ReturnsEmptyResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(page: 1, pageSize: 10);

        // Assert
        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    #endregion

    #region SearchByNameAsync Tests

    [Fact]
    public async Task SearchByNameAsync_MatchingClients_ReturnsFilteredClients()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        var clients = new[]
        {
            Client.Create("John Doe", Email.Create("john@example.com")),
            Client.Create("Jane Doe", Email.Create("jane@example.com")),
            Client.Create("Bob Smith", Email.Create("bob@example.com"))
        };
        await context.Clients.AddRangeAsync(clients);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.SearchByNameAsync("Doe");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Contains("Doe", c.Name));
    }

    [Fact]
    public async Task SearchByNameAsync_NoMatch_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        var client = Client.Create("John Doe", Email.Create("john@example.com"));
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.SearchByNameAsync("Smith");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchByNameAsync_PartialMatch_ReturnsMatchingClients()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ClientRepository(context);

        var client = Client.Create("John Doe", Email.Create("john@example.com"));
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.SearchByNameAsync("John");

        // Assert
        Assert.Single(result);
        Assert.Equal("John Doe", result[0].Name);
    }

    #endregion
}
