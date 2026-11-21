namespace Penny.Domain.Categories;

public sealed class Subcategory
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }
}
