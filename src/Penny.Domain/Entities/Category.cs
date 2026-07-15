using System;
using System.Collections.Generic;

namespace Penny.Domain.Entities;

public class Category
{
    private readonly List<Category> _subcategories = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }
    public Guid? UserId { get; private set; }

    // Navigation properties
    public Category? ParentCategory { get; private set; }
    public IReadOnlyCollection<Category> Subcategories => _subcategories.AsReadOnly();
    public User? User { get; private set; }

    // EF Core constructor
    private Category() { }

    public Category(Guid id, string name, Guid? parentCategoryId, Guid? userId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Id = id;
        Name = name;
        ParentCategoryId = parentCategoryId;
        UserId = userId;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
    }

    public void AddSubcategory(Category subcategory)
    {
        if (subcategory == null)
            throw new ArgumentNullException(nameof(subcategory));

        // Rule 1: A subcategory cannot have subcategories of its own
        if (ParentCategoryId != null)
            throw new InvalidOperationException("A subcategory cannot have subcategories of its own. Maximum of two levels is allowed.");

        // Rule 2: The subcategory being added cannot already be a parent category
        if (subcategory.Subcategories.Count > 0)
            throw new InvalidOperationException("Cannot add a parent category (which has its own subcategories) as a subcategory.");

        // Rule 3: The subcategory being added cannot be assigned to another parent
        if (subcategory.ParentCategoryId != null && subcategory.ParentCategoryId != Id)
            throw new InvalidOperationException("This category is already a subcategory of another category.");

        subcategory.ParentCategoryId = Id;
        subcategory.ParentCategory = this;

        if (!_subcategories.Contains(subcategory))
        {
            _subcategories.Add(subcategory);
        }
    }
}
