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

        var account = AccountMapper.ToEntity(request, userId);

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, AccountMapper.ToResponse(account));


    }
}