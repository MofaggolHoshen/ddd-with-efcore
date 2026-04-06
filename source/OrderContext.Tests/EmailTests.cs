using OrderContext.Domain;

namespace OrderContext.Tests;

public class EmailTests
{
    #region Validation Tests

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("a@b.co")]
    public void Create_WithValidEmail_ReturnsEmailInstance(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);

        // Assert
        Assert.NotNull(email);
        Assert.Equal(validEmail.ToLowerInvariant(), email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_WithNullOrWhitespace_ThrowsArgumentException(string? invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail!));
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("plaintext")]
    [InlineData("@nodomain.com")]
    [InlineData("missing@.com")]
    [InlineData("missing@domain")]
    [InlineData("spaces in@email.com")]
    [InlineData("double@@at.com")]
    public void Create_WithInvalidFormat_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        Assert.Contains("Invalid email format", exception.Message);
    }

    [Fact]
    public void Create_WithEmailExceeding254Characters_ThrowsArgumentException()
    {
        // Arrange
        var longLocalPart = new string('a', 250);
        var tooLongEmail = $"{longLocalPart}@test.com";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(tooLongEmail));
        Assert.Contains("maximum length", exception.Message);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        // Arrange
        var emailWithSpaces = "  test@example.com  ";

        // Act
        var email = Email.Create(emailWithSpaces);

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Create_ConvertsToLowercase()
    {
        // Arrange
        var mixedCaseEmail = "Test.User@EXAMPLE.COM";

        // Act
        var email = Email.Create(mixedCaseEmail);

        // Assert
        Assert.Equal("test.user@example.com", email.Value);
    }

    #endregion

    #region Value Object Equality Tests

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        // Act & Assert
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        Assert.NotEqual(email1, email2);
    }

    [Fact]
    public void Equals_WithDifferentCasing_ReturnsTrue()
    {
        // Arrange - emails are normalized to lowercase
        var email1 = Email.Create("TEST@example.com");
        var email2 = Email.Create("test@EXAMPLE.com");

        // Act & Assert
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void GetHashCode_SameEmails_ReturnsSameHashCode()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        // Act & Assert
        Assert.Equal(email1.GetHashCode(), email2.GetHashCode());
    }

    #endregion
}
