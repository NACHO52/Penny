namespace Penny.Domain.Category;

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string UserId { get; set; }
}