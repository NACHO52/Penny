namespace Penny.Domain.Categories;

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string UserId { get; set; }
}