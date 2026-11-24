namespace Penny.Domain.Movement;

public sealed class Movement
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public required MovementType Type { get; set; }
    public required DateTime MovementDate { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public int CategoryId { get; set; }
    public int? SubcategoryId { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
}