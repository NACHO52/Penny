using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Penny.Infrastructure.Data;

namespace Penny.Api.Account;

[ApiController]
[Authorize]
[Route("account")]
public sealed class AccountController : ControllerBase
{
    private const string NameCollation = "Latin1_General_100_CI_AS";

    private readonly PennyDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(PennyDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var accounts = await _db.Accounts
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.Id)
            .Select(account => new AccountResponseDTO(
                account.Id,
                account.Name,
                account.CurrencyCode,
                account.InitialBalance))
            .ToListAsync(cancellationToken);

        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        CreateAccountRequestDTO request,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var name = request.Name?.Trim();
        var currency = request.CurrencyCode;
        var balance = request.InitialBalance;

        if (string.IsNullOrWhiteSpace(name) ||
            !Enum.IsDefined(currency) ||
            balance is < -9_999_999_999_999_999.99m or > 9_999_999_999_999_999.99m ||
            decimal.Round(balance, 2) != balance)
        {
            return BadRequest(new { error = "Enter a name, a valid currency and an initial balance with up to two decimal places." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (!await _db.LockUserAsync(userId, cancellationToken))
        {
            return Unauthorized();
        }

        if (await NameExistsAsync(userId, name, null, cancellationToken))
        {
            return Conflict(new { error = "An account with that name already exists." });
        }

        var account = AccountMapper.ToEntity(request, userId);
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, AccountMapper.ToResponse(account));
    }

    [HttpPatch("{accountId:int}")]
    public async Task<IActionResult> RenameAccount(
        int accountId, AccountNameRequestDTO request, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Enter an account name." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (!await _db.LockUserAsync(userId, cancellationToken))
        {
            return Unauthorized();
        }

        var target = _db.Accounts.Where(account => account.Id == accountId && account.UserId == userId);
        if (!await target.AnyAsync(cancellationToken))
        {
            return NotFound();
        }

        if (await NameExistsAsync(userId, name, accountId, cancellationToken))
        {
            return Conflict(new { error = "An account with that name already exists." });
        }

        var updated = await target.ExecuteUpdateAsync(
            fields => fields.SetProperty(account => account.Name, name), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return updated == 0 ? NotFound() : NoContent();
    }

    private Task<bool> NameExistsAsync(string userId, string name, int? excludedId, CancellationToken cancellationToken)
    {
        return _db.Accounts.AnyAsync(account => account.UserId == userId && account.Id != excludedId &&
            EF.Functions.Collate(account.Name.Trim(), NameCollation) == name, cancellationToken);
    }
}
