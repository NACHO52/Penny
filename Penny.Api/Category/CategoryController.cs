using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penny.Infrastructure.Data;

namespace Penny.Api.Category;

[ApiController]
[Authorize]
[Route("category")]
public sealed class CategoryController : ControllerBase
{
    private readonly PennyDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public CategoryController(PennyDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var categories = await _db.Categories
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryResponseDTO(
                category.Id,
                category.Name,
                _db.Subcategories
                    .Where(subcategory => subcategory.CategoryId == category.Id)
                    .OrderBy(subcategory => subcategory.Name)
                    .ThenBy(subcategory => subcategory.Id)
                    .Select(subcategory => new SubcategoryResponseDTO(
                        subcategory.Id,
                        subcategory.Name))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}