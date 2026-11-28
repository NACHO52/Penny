using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Penny.Infrastructure.Data;
using Penny.Infrastructure.Reports;

namespace Penny.Api.Reports;

[ApiController]
[Authorize]
[Route("report")]
public sealed class ReportController(PennyDbContext db, UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpGet("monthly-expenses")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetMonthlyExpenses(
        [FromQuery] int accountId,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (accountId <= 0 || year is < 1 or > 9999)
        {
            return BadRequest(new { error = "Enter a valid account ID and year." });
        }

        var report = await db.GetMonthlyExpenseReportAsync(userId, accountId, year, cancellationToken);
        if (report is null)
        {
            return NotFound(new { error = "Account not found." });
        }

        return Ok(new
        {
            report.FirstYear,
            report.LastYear,
            Rows = report.Rows.Select(row => new
            {
                row.RowKind,
                row.CategoryId,
                row.SubcategoryId,
                row.Name,
                MonthlyAmounts = row.MonthlyAmounts.Select(FormatAmount),
                AnnualTotal = FormatAmount(row.AnnualTotal),
                row.AnnualShare
            })
        });
    }

    private static string FormatAmount(decimal amount) =>
        amount.ToString("F2", CultureInfo.InvariantCulture);
}
