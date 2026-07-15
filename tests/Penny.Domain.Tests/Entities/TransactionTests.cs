using System;
using Penny.Domain.Entities;
using Xunit;

namespace Penny.Domain.Tests.Entities;

public class TransactionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTransaction()
    {
        // Arrange
        var id = Guid.NewGuid();
        var amount = 150.50m;
        var description = "Supermarket";
        var date = DateTime.UtcNow;
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var transaction = new Transaction(id, amount, description, date, categoryId, userId);

        // Assert
        Assert.Equal(id, transaction.Id);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(description, transaction.Description);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(categoryId, transaction.CategoryId);
        Assert.Equal(userId, transaction.UserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WithInvalidAmount_ShouldThrowArgumentException(decimal amount)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Transaction(
            Guid.NewGuid(), amount, "Test", DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithEmptyCategoryId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Transaction(
            Guid.NewGuid(), 10m, "Test", DateTime.UtcNow, Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Transaction(
            Guid.NewGuid(), 10m, "Test", DateTime.UtcNow, Guid.NewGuid(), Guid.Empty));
    }
}
