using Penny.Domain.Movement;

namespace Penny.Api.Movement;

public sealed record MovementResponseDTO(
    int Id,
    int AccountId,
    MovementType Type,
    DateOnly MovementDate,
    int? CategoryId,
    int? SubcategoryId,
    decimal Amount,
    string Description);
