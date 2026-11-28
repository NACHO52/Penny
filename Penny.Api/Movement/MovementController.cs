using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penny.Domain.Movement;
using Penny.Infrastructure.Data;

namespace Penny.Api.Movement;

[ApiController]
[Authorize]
[Route("movement")]
public sealed class MovementController : ControllerBase
{
    private const decimal MaxAmount = 9_999_999_999_999_999.99m;

    private readonly PennyDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public MovementController(PennyDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMovement(
        MovementRequestDTO request,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var validationError = await ValidateMovementAsync(request, userId, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var movement = MovementMapper.ToEntity(request);
        _db.Movements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, MovementMapper.ToResponse(movement));
    }

    [HttpPut("{movementId:int}")]
    public async Task<IActionResult> UpdateMovement(
        int movementId, MovementRequestDTO request, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var movement = await _db.Movements.SingleOrDefaultAsync(
            movement => movement.Id == movementId && movement.AccountId == request.AccountId &&
                _db.Accounts.Any(account => account.Id == movement.AccountId && account.UserId == userId),
            cancellationToken);
        if (movement is null)
        {
            return NotFound(new { error = "Movement not found." });
        }

        var validationError = await ValidateMovementAsync(request, userId, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var updatedMovement = MovementMapper.ToEntity(request);
        updatedMovement.Id = movement.Id;
        updatedMovement.CreationDate = movement.CreationDate;
        var movementEntry = _db.Entry(movement);
        movementEntry.CurrentValues.SetValues(updatedMovement);
        movementEntry.State = EntityState.Modified;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(MovementMapper.ToResponse(movement));
    }

    private async Task<IActionResult?> ValidateMovementAsync(
        MovementRequestDTO request, string userId, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Type) ||
            string.IsNullOrWhiteSpace(request.Description) ||
            request.Amount <= 0 ||
            request.Amount > MaxAmount ||
            decimal.Round(request.Amount, 2) != request.Amount)
        {
            return BadRequest(new
            {
                error = "Enter a valid type, a description and a positive amount with up to two decimal places."
            });
        }

        var ownsAccount = await _db.Accounts.AnyAsync(
            account => account.Id == request.AccountId && account.UserId == userId,
            cancellationToken);

        if (!ownsAccount)
        {
            return NotFound(new { error = "Account not found." });
        }

        if (request.Type == MovementType.Expense)
        {
            if (request.CategoryId is not int categoryId ||
                !await _db.Categories.AnyAsync(
                    category => category.Id == categoryId && category.UserId == userId, cancellationToken))
            {
                return BadRequest(new { code = "invalid_category", error = "Select a valid category." });
            }

            if (request.SubcategoryId is int subcategoryId &&
                !await _db.Subcategories.AnyAsync(
                    subcategory => subcategory.Id == subcategoryId && subcategory.CategoryId == categoryId,
                    cancellationToken))
            {
                return BadRequest(new { code = "invalid_subcategory", error = "Select a subcategory from the selected category." });
            }
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetMovements(
    [FromQuery] int accountId,
    CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (accountId <= 0)
        {
            return BadRequest(new { error = "Enter a valid account ID." });
        }

        var ownsAccount = await _db.Accounts.AnyAsync(
            account => account.Id == accountId && account.UserId == userId,
            cancellationToken);

        if (!ownsAccount)
        {
            return NotFound(new { error = "Account not found." });
        }

        var movements = await _db.Movements
            .Where(movement => movement.AccountId == accountId)
            .OrderByDescending(movement => movement.MovementDate)
            .ThenByDescending(movement => movement.Id)
            .Select(movement => new MovementResponseDTO(
                movement.Id,
                movement.AccountId,
                movement.Type,
                DateOnly.FromDateTime(movement.MovementDate),
                movement.CategoryId,
                movement.SubcategoryId,
                movement.Amount,
                movement.Description))
            .ToListAsync(cancellationToken);

        return Ok(movements);
    }
}
