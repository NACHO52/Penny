namespace Penny.Domain.Accounts;

public sealed class Account
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required CurrencyCode CurrencyCode { get; set; }
    public decimal InitialBalance { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public required string UserId { get; set; }
}