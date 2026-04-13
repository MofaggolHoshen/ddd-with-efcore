using Microsoft.EntityFrameworkCore;
using OrderContext.Domain;
using OrderContext.Infratructure;
using OrderContext.Infratructure.Repositories;

namespace OrderContext.Tests;

/// <summary>
/// Unit tests for UnitOfWork implementation.
/// Tests transaction management and repository coordination.
/// </summary>
public class UnitOfWorkTests
{
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_AfterAddingClient_PersistsChanges()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);
        var email = Email.Create("test@example.com");
        var client = Client.Create("Test Client", email);

        // Act
        await unitOfWork.Clients.AddAsync(client);
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        var persisted = await context.Clients.FindAsync(client.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleOperations_PersistsAllChanges()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);

        var client1 = Client.Create("Client 1", Email.Create("c1@example.com"));
        var client2 = Client.Create("Client 2", Email.Create("c2@example.com"));

        // Act
        await unitOfWork.Clients.AddAsync(client1);
        await unitOfWork.Clients.AddAsync(client2);
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(2, result);
        Assert.Equal(2, await context.Clients.CountAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutChanges_ReturnsZero()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Clients Repository Property Tests

    [Fact]
    public void Clients_Property_ReturnsClientRepository()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);

        // Act
        var repository = unitOfWork.Clients;

        // Assert
        Assert.NotNull(repository);
    }

    [Fact]
    public void Clients_Property_ReturnsSameInstanceOnMultipleCalls()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);

        // Act
        var repository1 = unitOfWork.Clients;
        var repository2 = unitOfWork.Clients;

        // Assert
        Assert.Same(repository1, repository2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task UnitOfWork_AddAndRetrieveClient_WorksCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);
        var email = Email.Create("integration@example.com");
        var client = Client.Create("Integration Test", email);

        // Act
        await unitOfWork.Clients.AddAsync(client);
        await unitOfWork.SaveChangesAsync();
        var retrieved = await unitOfWork.Clients.GetByIdAsync(client.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Integration Test", retrieved.Name);
        Assert.Equal("integration@example.com", retrieved.Email.Value);
    }

    [Fact]
    public async Task UnitOfWork_UpdateClient_PersistsChanges()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);
        var email = Email.Create("update@example.com");
        var client = Client.Create("Original Name", email);
        await unitOfWork.Clients.AddAsync(client);
        await unitOfWork.SaveChangesAsync();

        // Act
        client.UpdateName("Updated Name");
        await unitOfWork.SaveChangesAsync();

        // Assert
        context.ChangeTracker.Clear();
        var updated = await context.Clients.FindAsync(client.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UnitOfWork_DeleteClient_RemovesFromDatabase()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);
        var email = Email.Create("delete@example.com");
        var client = Client.Create("Delete Me", email);
        await unitOfWork.Clients.AddAsync(client);
        await unitOfWork.SaveChangesAsync();

        // Act
        unitOfWork.Clients.Remove(client);
        await unitOfWork.SaveChangesAsync();

        // Assert
        var deleted = await context.Clients.FindAsync(client.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task UnitOfWork_CheckEmailExists_WorksThroughRepository()
    {
        // Arrange
        using var context = CreateDbContext();
        using var unitOfWork = new UnitOfWork(context);
        var email = Email.Create("exists@example.com");
        var client = Client.Create("Exists Client", email);
        await unitOfWork.Clients.AddAsync(client);
        await unitOfWork.SaveChangesAsync();

        // Act
        var exists = await unitOfWork.Clients.EmailExistsAsync(email);
        var notExists = await unitOfWork.Clients.EmailExistsAsync(Email.Create("notexists@example.com"));

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var context = CreateDbContext();
        var unitOfWork = new UnitOfWork(context);

        // Act & Assert
        unitOfWork.Dispose();
        unitOfWork.Dispose(); // Should not throw
    }

    #endregion
}
