using Moq;
using OrderContext.Domain;
using OrderContext.Domain.Common;
using OrderContext.Domain.Services;

namespace OrderContext.Tests;

public class ClientRegistrationServiceTests
{
    private readonly Mock<IEmailUniquenessChecker> _emailCheckerMock;
    private readonly ClientRegistrationService _service;

    public ClientRegistrationServiceTests()
    {
        _emailCheckerMock = new Mock<IEmailUniquenessChecker>();
        _service = new ClientRegistrationService(_emailCheckerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullEmailChecker_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClientRegistrationService(null!));
    }

    #endregion

    #region RegisterClient Tests

    [Fact]
    public void RegisterClient_WithUniqueEmail_ReturnsClient()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        _emailCheckerMock
            .Setup(x => x.IsEmailUnique(email))
            .Returns(true);

        // Act
        var client = _service.RegisterClient("John Doe", email);

        // Assert
        Assert.NotNull(client);
        Assert.Equal("John Doe", client.Name);
        Assert.Equal(email, client.Email);
    }

    [Fact]
    public void RegisterClient_WithDuplicateEmail_ThrowsDomainException()
    {
        // Arrange
        var email = Email.Create("existing@example.com");
        _emailCheckerMock
            .Setup(x => x.IsEmailUnique(email))
            .Returns(false);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(
            () => _service.RegisterClient("Jane Doe", email));

        Assert.Contains("already exists", exception.Message);
        Assert.Contains(email.Value, exception.Message);
    }

    [Fact]
    public void RegisterClient_WithNullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _service.RegisterClient("John Doe", null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterClient_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange
        var email = Email.Create("test@example.com");
        _emailCheckerMock
            .Setup(x => x.IsEmailUnique(email))
            .Returns(true);

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _service.RegisterClient(invalidName!, email));
    }

    [Fact]
    public void RegisterClient_CallsEmailUniquenessChecker()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        _emailCheckerMock
            .Setup(x => x.IsEmailUnique(email))
            .Returns(true);

        // Act
        _service.RegisterClient("John Doe", email);

        // Assert
        _emailCheckerMock.Verify(x => x.IsEmailUnique(email), Times.Once);
    }

    [Fact]
    public void RegisterClient_WithUniqueEmail_CreatesClientWithNewId()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        _emailCheckerMock
            .Setup(x => x.IsEmailUnique(email))
            .Returns(true);

        // Act
        var client = _service.RegisterClient("John Doe", email);

        // Assert
        Assert.NotEqual(Guid.Empty, client.Id);
    }

    [Fact]
    public void RegisterClient_WithUniqueEmail_SetsCreatedAt()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var beforeCreation = DateTime.UtcNow;
        _emailCheckerMock
            .Setup(x => x.IsEmailUnique(email))
            .Returns(true);

        // Act
        var client = _service.RegisterClient("John Doe", email);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.InRange(client.CreatedAt, beforeCreation, afterCreation);
    }

    #endregion
}
