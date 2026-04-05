using Microsoft.EntityFrameworkCore;
using OrderContext.Domain;
using OrderContext.Infratructure;

namespace OrderContext.Tests;

public class ClientConfigurationTests
{
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    #region Email Value Conversion Tests

    [Fact]
    public void SaveClient_WithEmail_PersistsEmailValue()
    {
        // Arrange
        using var context = CreateDbContext();
        var email = Email.Create("persistence@test.com");
        var client = Client.Create("Test User", email);

        // Act
        context.Clients.Add(client);
        context.SaveChanges();

        // Assert
        context.ChangeTracker.Clear();
        var savedClient = context.Clients.First();
        Assert.Equal("persistence@test.com", savedClient.Email.Value);
    }

    [Fact]
    public void UpdateClient_EmailChange_PersistsNewValue()
    {
        // Arrange
        using var context = CreateDbContext();
        var originalEmail = Email.Create("original@test.com");
        var client = Client.Create("Test User", originalEmail);
        context.Clients.Add(client);
        context.SaveChanges();

        // Act
        var newEmail = Email.Create("updated@test.com");
        client.UpdateEmail(newEmail);
        context.SaveChanges();

        // Assert
        context.ChangeTracker.Clear();
        var updatedClient = context.Clients.First(c => c.Id == client.Id);
        Assert.Equal("updated@test.com", updatedClient.Email.Value);
    }

    [Fact]
    public void MultipleClients_WithDifferentEmails_AllPersistCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var clients = new[]
        {
            Client.Create("User 1", Email.Create("user1@test.com")),
            Client.Create("User 2", Email.Create("user2@test.com")),
            Client.Create("User 3", Email.Create("user3@test.com"))
        };

        // Act
        context.Clients.AddRange(clients);
        context.SaveChanges();

        // Assert
        context.ChangeTracker.Clear();
        var savedClients = context.Clients.OrderBy(c => c.Name).ToList();

        Assert.Equal(3, savedClients.Count);
        Assert.Equal("user1@test.com", savedClients[0].Email.Value);
        Assert.Equal("user2@test.com", savedClients[1].Email.Value);
        Assert.Equal("user3@test.com", savedClients[2].Email.Value);
    }

    #endregion

    #region Entity Configuration Tests

    [Fact]
    public void ClientName_HasMaxLength200()
    {
        // Arrange
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var nameProperty = entityType?.FindProperty(nameof(Client.Name));

        // Assert
        Assert.NotNull(nameProperty);
        Assert.Equal(200, nameProperty.GetMaxLength());
    }

    [Fact]
    public void ClientName_IsRequired()
    {
        // Arrange
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var nameProperty = entityType?.FindProperty(nameof(Client.Name));

        // Assert
        Assert.NotNull(nameProperty);
        Assert.False(nameProperty.IsNullable);
    }

    [Fact]
    public void ClientCreatedAt_IsRequired()
    {
        // Arrange
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var createdAtProperty = entityType?.FindProperty(nameof(Client.CreatedAt));

        // Assert
        Assert.NotNull(createdAtProperty);
        Assert.False(createdAtProperty.IsNullable);
    }

    [Fact]
    public void Email_IsConfiguredWithValueConversion()
    {
        // Arrange
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var emailProperty = entityType?.FindProperty(nameof(Client.Email));

        // Assert
        Assert.NotNull(emailProperty);
        Assert.NotNull(emailProperty.GetValueConverter());
    }

    [Fact]
    public void EmailProperty_HasMaxLength254()
    {
        // Arrange
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var emailProperty = entityType?.FindProperty(nameof(Client.Email));

        // Assert
        Assert.NotNull(emailProperty);
        Assert.Equal(254, emailProperty.GetMaxLength());
    }

    [Fact]
    public void EmailProperty_IsRequired()
    {
        // Arrange
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Client));
        var emailProperty = entityType?.FindProperty(nameof(Client.Email));

        // Assert
        Assert.NotNull(emailProperty);
        Assert.False(emailProperty.IsNullable);
    }

    #endregion

    #region Tracking and Change Detection Tests

    [Fact]
    public void ModifyEmail_TracksChanges()
    {
        // Arrange
        using var context = CreateDbContext();
        var client = Client.Create("Test User", Email.Create("original@test.com"));
        context.Clients.Add(client);
        context.SaveChanges();

        // Act
        client.UpdateEmail(Email.Create("modified@test.com"));
        var entry = context.Entry(client);

        // Assert
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion
}
