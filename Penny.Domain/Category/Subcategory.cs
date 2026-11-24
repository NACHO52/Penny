namespace Penny.Domain.Category;

public sealed class Subcategory
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }
}
