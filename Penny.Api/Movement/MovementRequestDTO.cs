using Penny.Domain.Movement;

namespace Penny.Api.Movement;

public sealed record MovementRequestDTO
{
    public required int AccountId { get; init; }
    public required MovementType Type { get; init; }
    public required DateOnly MovementDate { get; init; }
    public int? CategoryId { get; init; }
    public int? SubcategoryId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
}
