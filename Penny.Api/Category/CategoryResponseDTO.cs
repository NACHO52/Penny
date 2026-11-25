namespace Penny.Api.Category;

public sealed record CategoryResponseDTO(
    int Id,
    string Name,
    IReadOnlyList<SubcategoryResponseDTO> Subcategories);