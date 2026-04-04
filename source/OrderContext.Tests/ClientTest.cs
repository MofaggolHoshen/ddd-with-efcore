using Microsoft.EntityFrameworkCore;
using OrderContext.Domain;
using OrderContext.Infratructure;

namespace OrderContext.Tests;

public class ClientTest
{
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    [Fact]
    public void CreateClient_Succeeds()
    {
        // Arrange
        var email = Email.Create("Mofaggol@bd.com");
        var name = "Mofaggol Hoshen";

        // Act
        var clinet = Client.Create(name, email);

        // Assert
        Assert.NotNull(clinet);
        Assert.NotEqual(Guid.Empty, clinet.Id);
        Assert.Equal(name, clinet.Name);
        Assert.Equal(email.Value, clinet.Email.Value);
        Assert.True(clinet.CreatedAt <= DateTime.UtcNow);
    }


    [Fact]
    public void PersistClient_Saves()
    {
        // Arrange
        using var context = CreateDbContext();
        var email = Email.Create("Mofaggol.Hoshen@bd.com");
        var client = Client.Create("Mofaggol Hoshen", email);

        // Act
        context.Clients.Add(client);
        context.SaveChanges();

        // Assert
        var savedClient = context.Clients.FirstOrDefault();
        Assert.NotNull(savedClient);
        Assert.Equal(client.Id, savedClient.Id);
        Assert.Equal(client.Name, savedClient.Name);
        Assert.Equal(client.Email.Value, savedClient.Email.Value);
        Assert.Equal(client.CreatedAt, savedClient.CreatedAt);
    }

    [Fact]
    public void UpdateClientEmail_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var originalEmail = Email.Create("Mofaggol.hoshen@bd.com");
        var client = Client.Create("Mofaggol Hoshen", originalEmail);
        context.Clients.Add(client);
        context.SaveChanges();

        // Act
        var newEmail = Email.Create("Hoshen.Mofaggol@bd.com");
        client.UpdateEmail(newEmail);
        context.SaveChanges();

        // Assert
        var updatedClient = context.Clients.FirstOrDefault();
        Assert.NotNull(updatedClient);
        Assert.Equal(newEmail.Value, updatedClient.Email.Value);
        Assert.Equal(client.Name, updatedClient.Name);
        Assert.Equal(client.Id, updatedClient.Id);
    }
}
