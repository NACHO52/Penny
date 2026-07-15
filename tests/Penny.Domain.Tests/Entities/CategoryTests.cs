using System;
using Penny.Domain.Entities;
using Xunit;

namespace Penny.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateCategory()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Food";
        var parentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var category = new Category(id, name, parentId, userId);

        // Assert
        Assert.Equal(id, category.Id);
        Assert.Equal(name, category.Name);
        Assert.Equal(parentId, category.ParentCategoryId);
        Assert.Equal(userId, category.UserId);
    }

    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Category(Guid.Empty, "Food", null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Category(Guid.NewGuid(), name, null, null));
    }

    [Fact]
    public void AddSubcategory_WithValidSubcategory_ShouldAddSubcategory()
    {
        // Arrange
        var parent = new Category(Guid.NewGuid(), "Ocio", null, Guid.NewGuid());
        var subcategory = new Category(Guid.NewGuid(), "Restaurantes", null, Guid.NewGuid());

        // Act
        parent.AddSubcategory(subcategory);

        // Assert
        Assert.Contains(subcategory, parent.Subcategories);
        Assert.Equal(parent.Id, subcategory.ParentCategoryId);
        Assert.Equal(parent, subcategory.ParentCategory);
    }

    [Fact]
    public void AddSubcategory_WhenCategoryIsAlreadyASubcategory_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var grandParent = new Category(Guid.NewGuid(), "Ocio", null, Guid.NewGuid());
        var parent = new Category(Guid.NewGuid(), "Restaurantes", grandParent.Id, Guid.NewGuid());
        var child = new Category(Guid.NewGuid(), "Comida Rápida", null, Guid.NewGuid());

        // Act & Assert: parent already has a ParentCategoryId (it is a subcategory)
        // so calling AddSubcategory on it must fail.
        var exception = Assert.Throws<InvalidOperationException>(() => parent.AddSubcategory(child));
        Assert.Contains("Maximum of two levels is allowed", exception.Message);
    }

    [Fact]
    public void AddSubcategory_WhenSubcategoryAlreadyHasItsOwnSubcategories_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var parent1 = new Category(Guid.NewGuid(), "Ocio", null, Guid.NewGuid());
        var subcategory = new Category(Guid.NewGuid(), "Restaurantes", null, Guid.NewGuid());
        var leaf = new Category(Guid.NewGuid(), "Comida Rápida", null, Guid.NewGuid());

        // subcategory becomes a parent of leaf
        subcategory.AddSubcategory(leaf);

        // Act & Assert: trying to add subcategory (which has its own subcategories) as a subcategory of parent1
        var exception = Assert.Throws<InvalidOperationException>(() => parent1.AddSubcategory(subcategory));
        Assert.Contains("Cannot add a parent category", exception.Message);
    }

    [Fact]
    public void AddSubcategory_WhenSubcategoryIsAlreadySubcategoryOfAnotherCategory_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var parent1 = new Category(Guid.NewGuid(), "Ocio", null, Guid.NewGuid());
        var parent2 = new Category(Guid.NewGuid(), "Hogar", null, Guid.NewGuid());
        var subcategory = new Category(Guid.NewGuid(), "Restaurantes", null, Guid.NewGuid());

        parent1.AddSubcategory(subcategory);

        // Act & Assert: trying to add the same subcategory to another parent
        var exception = Assert.Throws<InvalidOperationException>(() => parent2.AddSubcategory(subcategory));
        Assert.Contains("already a subcategory of another category", exception.Message);
    }
}
