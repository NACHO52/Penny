using MovementEntity = Penny.Domain.Movement.Movement;

namespace Penny.Api.Movement;

internal static class MovementMapper
{
    public static MovementEntity ToEntity(CreateMovementRequestDTO request)
    {
        return new MovementEntity
        {
            AccountId = request.AccountId,
            Type = request.Type,
            MovementDate = request.MovementDate.ToDateTime(TimeOnly.MinValue),
            CategoryId = request.CategoryId,
            SubcategoryId = request.SubcategoryId,
            Amount = request.Amount,
            Description = request.Description.Trim()
        };
    }
    public static MovementResponseDTO ToResponse(MovementEntity movement)
    {
        return new MovementResponseDTO(
            movement.Id,
            movement.AccountId,
            movement.Type,
            DateOnly.FromDateTime(movement.MovementDate),
            movement.CategoryId,
            movement.SubcategoryId,
            movement.Amount,
            movement.Description);
    }
}