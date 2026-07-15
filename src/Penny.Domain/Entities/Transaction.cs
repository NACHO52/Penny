using System;

namespace Penny.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public Category Category { get; private set; } = null!;
    public User User { get; private set; } = null!;

    // EF Core constructor
    private Transaction() { }

    public Transaction(Guid id, decimal amount, string description, DateTime date, Guid categoryId, Guid userId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        Id = id;
        Amount = amount;
        Description = description ?? string.Empty;
        Date = date;
        CategoryId = categoryId;
        UserId = userId;
    }

    public void Update(decimal amount, string description, DateTime date, Guid categoryId)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));

        Amount = amount;
        Description = description ?? string.Empty;
        Date = date;
        CategoryId = categoryId;
    }
}
