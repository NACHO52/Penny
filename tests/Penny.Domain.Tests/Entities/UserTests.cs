using System;
using Penny.Domain.Entities;
using Xunit;

namespace Penny.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateUser()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "John Doe";
        var email = "john@example.com";

        // Act
        var user = new User(id, name, email);

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
    }

    [Theory]
    [InlineData("", "john@example.com")]
    [InlineData(" ", "john@example.com")]
    [InlineData("John", "")]
    [InlineData("John", " ")]
    public void Constructor_WithInvalidParameters_ShouldThrowArgumentException(string name, string email)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new User(Guid.NewGuid(), name, email));
    }

    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new User(Guid.Empty, "John", "john@example.com"));
    }
}
