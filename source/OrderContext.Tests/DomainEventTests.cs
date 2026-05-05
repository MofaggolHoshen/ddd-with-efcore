using Microsoft.EntityFrameworkCore;
using Moq;
using OrderContext.Domain;
using OrderContext.Domain.Common;
using OrderContext.Domain.Events;
using OrderContext.Infratructure;

namespace OrderContext.Tests;

/// <summary>
/// Tests that domain events are raised correctly by the Client entity
/// and dispatched by OrderDbContext on SaveChangesAsync.
/// </summary>
public class DomainEventTests
{
    #region Client.Create raises ClientRegisteredEvent

    [Fact]
    public void Create_Client_RaisesClientRegisteredEvent()
    {
        // Act
        var client = Client.Create("Alice", Email.Create("alice@example.com"));

        // Assert
        var evt = Assert.Single(client.DomainEvents);
        var registered = Assert.IsType<ClientRegisteredEvent>(evt);
        Assert.Equal(client.Id, registered.ClientId);
        Assert.Equal("Alice", registered.Name);
        Assert.Equal("alice@example.com", registered.Email);
    }

    [Fact]
    public void Create_Client_EventOccurredOn_IsUtcNow()
    {
        var before = DateTime.UtcNow;
        var client = Client.Create("Bob", Email.Create("bob@example.com"));
        var after = DateTime.UtcNow;

        var evt = Assert.IsType<ClientRegisteredEvent>(client.DomainEvents[0]);
        Assert.InRange(evt.OccurredOn, before, after);
    }

    #endregion

    #region UpdateName raises ClientNameChangedEvent

    [Fact]
    public void UpdateName_RaisesClientNameChangedEvent()
    {
        // Arrange
        var client = Client.Create("Alice", Email.Create("alice@example.com"));
        client.ClearDomainEvents();

        // Act
        client.UpdateName("Alice Updated");

        // Assert
        var evt = Assert.Single(client.DomainEvents);
        var nameChanged = Assert.IsType<ClientNameChangedEvent>(evt);
        Assert.Equal(client.Id, nameChanged.ClientId);
        Assert.Equal("Alice", nameChanged.OldName);
        Assert.Equal("Alice Updated", nameChanged.NewName);
    }

    #endregion

    #region UpdateEmail raises ClientEmailChangedEvent

    [Fact]
    public void UpdateEmail_RaisesClientEmailChangedEvent()
    {
        // Arrange
        var client = Client.Create("Alice", Email.Create("alice@example.com"));
        client.ClearDomainEvents();

        // Act
        client.UpdateEmail(Email.Create("newalice@example.com"));

        // Assert
        var evt = Assert.Single(client.DomainEvents);
        var emailChanged = Assert.IsType<ClientEmailChangedEvent>(evt);
        Assert.Equal(client.Id, emailChanged.ClientId);
        Assert.Equal("alice@example.com", emailChanged.OldEmail);
        Assert.Equal("newalice@example.com", emailChanged.NewEmail);
    }

    #endregion

    #region ClearDomainEvents

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var client = Client.Create("Alice", Email.Create("alice@example.com"));
        Assert.NotEmpty(client.DomainEvents);

        // Act
        client.ClearDomainEvents();

        // Assert
        Assert.Empty(client.DomainEvents);
    }

    [Fact]
    public void MultipleOperations_AccumulateEvents()
    {
        // Arrange
        var client = Client.Create("Alice", Email.Create("alice@example.com"));

        // Act
        client.UpdateName("Alice B");
        client.UpdateEmail(Email.Create("aliceb@example.com"));

        // Assert: ClientRegistered + NameChanged + EmailChanged
        Assert.Equal(3, client.DomainEvents.Count);
        Assert.IsType<ClientRegisteredEvent>(client.DomainEvents[0]);
        Assert.IsType<ClientNameChangedEvent>(client.DomainEvents[1]);
        Assert.IsType<ClientEmailChangedEvent>(client.DomainEvents[2]);
    }

    #endregion

    #region OrderDbContext dispatches events on SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_DispatchesDomainEvents_AndClearsThemFromEntity()
    {
        // Arrange
        var dispatchedEvents = new List<IDomainEvent>();
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IDomainEvent, CancellationToken>((evt, _) => dispatchedEvents.Add(evt))
            .Returns(Task.CompletedTask);

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options, dispatcherMock.Object);

        var client = Client.Create("Alice", Email.Create("alice@example.com"));
        await context.Clients.AddAsync(client);

        // Act
        await context.SaveChangesAsync();

        // Assert: event was dispatched
        Assert.Single(dispatchedEvents);
        Assert.IsType<ClientRegisteredEvent>(dispatchedEvents[0]);

        // Assert: events cleared from entity after save
        Assert.Empty(client.DomainEvents);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutDispatcher_SavesSuccessfullyWithoutDispatching()
    {
        // Arrange — no dispatcher (simulates test contexts)
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var client = Client.Create("Bob", Email.Create("bob@example.com"));
        await context.Clients.AddAsync(client);

        // Act & Assert — should not throw
        var result = await context.SaveChangesAsync();
        Assert.Equal(1, result);
    }

    #endregion
}
