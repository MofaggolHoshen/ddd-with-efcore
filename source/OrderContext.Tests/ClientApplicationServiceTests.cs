using Microsoft.EntityFrameworkCore;
using OrderContext.Application.DTOs;
using OrderContext.Application.Services;
using OrderContext.Domain;
using OrderContext.Infratructure;
using OrderContext.Infratructure.Repositories;

namespace OrderContext.Tests;

/// <summary>
/// Unit tests for ClientApplicationService.
/// Tests application layer operations with in-memory database.
/// </summary>
public class ClientApplicationServiceTests
{
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    private (ClientApplicationService Service, UnitOfWork UnitOfWork) CreateService(OrderDbContext context)
    {
        var unitOfWork = new UnitOfWork(context);
        var service = new ClientApplicationService(unitOfWork);
        return (service, unitOfWork);
    }

    #region RegisterClientAsync Tests

    [Fact]
    public async Task RegisterClientAsync_ValidData_ReturnsClientId()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        // Act
        var clientId = await service.RegisterClientAsync("Test Client", "test@example.com");

        // Assert
        Assert.NotEqual(Guid.Empty, clientId);
        var client = await context.Clients.FindAsync(clientId);
        Assert.NotNull(client);
        Assert.Equal("Test Client", client.Name);
    }

    [Fact]
    public async Task RegisterClientAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        await service.RegisterClientAsync("First Client", "duplicate@example.com");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterClientAsync("Second Client", "duplicate@example.com"));
        Assert.Contains("duplicate@example.com", exception.Message);
    }

    #endregion

    #region GetClientByIdAsync Tests

    [Fact]
    public async Task GetClientByIdAsync_ExistingClient_ReturnsClientDto()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        var clientId = await service.RegisterClientAsync("Test Client", "test@example.com");

        // Act
        var result = await service.GetClientByIdAsync(clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(clientId, result.Id);
        Assert.Equal("Test Client", result.Name);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetClientByIdAsync_NonExistingClient_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        // Act
        var result = await service.GetClientByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllClientsAsync Tests

    [Fact]
    public async Task GetAllClientsAsync_WithClients_ReturnsAllClientDtos()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        await service.RegisterClientAsync("Client 1", "c1@example.com");
        await service.RegisterClientAsync("Client 2", "c2@example.com");
        await service.RegisterClientAsync("Client 3", "c3@example.com");

        // Act
        var result = await service.GetAllClientsAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllClientsAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        // Act
        var result = await service.GetAllClientsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region UpdateClientNameAsync Tests

    [Fact]
    public async Task UpdateClientNameAsync_ExistingClient_UpdatesName()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        var clientId = await service.RegisterClientAsync("Original Name", "test@example.com");

        // Act
        await service.UpdateClientNameAsync(clientId, "Updated Name");

        // Assert
        var updated = await service.GetClientByIdAsync(clientId);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateClientNameAsync_NonExistingClient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateClientNameAsync(Guid.NewGuid(), "New Name"));
    }

    #endregion

    #region UpdateClientEmailAsync Tests

    [Fact]
    public async Task UpdateClientEmailAsync_ExistingClient_UpdatesEmail()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        var clientId = await service.RegisterClientAsync("Test Client", "original@example.com");

        // Act
        await service.UpdateClientEmailAsync(clientId, "updated@example.com");

        // Assert
        var updated = await service.GetClientByIdAsync(clientId);
        Assert.NotNull(updated);
        Assert.Equal("updated@example.com", updated.Email);
    }

    [Fact]
    public async Task UpdateClientEmailAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        await service.RegisterClientAsync("Client 1", "client1@example.com");
        var client2Id = await service.RegisterClientAsync("Client 2", "client2@example.com");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateClientEmailAsync(client2Id, "client1@example.com"));
        Assert.Contains("client1@example.com", exception.Message);
    }

    [Fact]
    public async Task UpdateClientEmailAsync_NonExistingClient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateClientEmailAsync(Guid.NewGuid(), "new@example.com"));
    }

    #endregion

    #region DeleteClientAsync Tests

    [Fact]
    public async Task DeleteClientAsync_ExistingClient_RemovesClient()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        var clientId = await service.RegisterClientAsync("Delete Me", "delete@example.com");

        // Act
        await service.DeleteClientAsync(clientId);

        // Assert
        var deleted = await service.GetClientByIdAsync(clientId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteClientAsync_NonExistingClient_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteClientAsync(Guid.NewGuid()));
    }

    #endregion

    #region GetPagedClientsAsync Tests

    [Fact]
    public async Task GetPagedClientsAsync_FirstPage_ReturnsCorrectPagedResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        for (int i = 1; i <= 10; i++)
        {
            await service.RegisterClientAsync($"Client {i:D2}", $"client{i}@example.com");
        }

        // Act
        var result = await service.GetPagedClientsAsync(page: 1, pageSize: 3);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(4, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetPagedClientsAsync_MiddlePage_HasCorrectPaginationFlags()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        for (int i = 1; i <= 10; i++)
        {
            await service.RegisterClientAsync($"Client {i:D2}", $"client{i}@example.com");
        }

        // Act
        var result = await service.GetPagedClientsAsync(page: 2, pageSize: 3);

        // Assert
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetPagedClientsAsync_LastPage_HasCorrectPaginationFlags()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        for (int i = 1; i <= 10; i++)
        {
            await service.RegisterClientAsync($"Client {i:D2}", $"client{i}@example.com");
        }

        // Act
        var result = await service.GetPagedClientsAsync(page: 4, pageSize: 3);

        // Assert
        Assert.Single(result.Items);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetPagedClientsAsync_EmptyRepository_ReturnsEmptyPagedResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        // Act
        var result = await service.GetPagedClientsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    #endregion

    #region SearchClientsByNameAsync Tests

    [Fact]
    public async Task SearchClientsByNameAsync_MatchingClients_ReturnsFilteredResults()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        await service.RegisterClientAsync("John Doe", "john@example.com");
        await service.RegisterClientAsync("Jane Doe", "jane@example.com");
        await service.RegisterClientAsync("Bob Smith", "bob@example.com");

        // Act
        var result = await service.SearchClientsByNameAsync("Doe");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Contains("Doe", dto.Name));
    }

    [Fact]
    public async Task SearchClientsByNameAsync_NoMatch_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        await service.RegisterClientAsync("John Doe", "john@example.com");

        // Act
        var result = await service.SearchClientsByNameAsync("Smith");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetClientsCreatedBetweenAsync Tests

    [Fact]
    public async Task GetClientsCreatedBetweenAsync_WithMatchingClients_ReturnsFilteredResults()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        await service.RegisterClientAsync("Client 1", "c1@example.com");
        await service.RegisterClientAsync("Client 2", "c2@example.com");

        var startDate = DateTime.UtcNow.AddMinutes(-1);
        var endDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var result = await service.GetClientsCreatedBetweenAsync(startDate, endDate);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task GetClientByIdAsync_ReturnsCorrectlyMappedDto()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, unitOfWork) = CreateService(context);
        using var _ = unitOfWork;

        var clientId = await service.RegisterClientAsync("Test Client", "test@example.com");

        // Act
        var result = await service.GetClientByIdAsync(clientId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ClientDto>(result);
        Assert.Equal(clientId, result.Id);
        Assert.Equal("Test Client", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    #endregion
}
