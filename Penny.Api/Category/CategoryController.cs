using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Penny.Infrastructure.Data;

namespace Penny.Api.Category;

[ApiController]
[Authorize]
[Route("category")]
public sealed class CategoryController : ControllerBase
{
    private const string NameCollation = "Latin1_General_100_CI_AS";

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

    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        CreateCategoryRequestDTO request,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Enter a category name." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (!await LockOwnerAsync(userId, cancellationToken))
        {
            return Unauthorized();
        }

        var nameExists = await _db.Categories.AnyAsync(
            category => category.UserId == userId &&
                EF.Functions.Collate(category.Name.Trim(), NameCollation) == name,
            cancellationToken);
        if (nameExists)
        {
            return Conflict(new { error = "A category with that name already exists." });
        }

        var category = new Domain.Category.Category { Name = name, UserId = userId };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            new CategoryResponseDTO(category.Id, category.Name, []));
    }

    [HttpPost("{categoryId:int}/subcategory")]
    public async Task<IActionResult> CreateSubcategory(
        int categoryId,
        CreateCategoryRequestDTO request,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Enter a subcategory name." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (!await LockOwnerAsync(userId, cancellationToken))
        {
            return Unauthorized();
        }

        var ownsCategory = await _db.Categories.AnyAsync(
            category => category.Id == categoryId && category.UserId == userId,
            cancellationToken);
        if (!ownsCategory)
        {
            return NotFound(new { error = "Category not found." });
        }

        var nameExists = await _db.Subcategories.AnyAsync(
            subcategory => subcategory.CategoryId == categoryId &&
                EF.Functions.Collate(subcategory.Name.Trim(), NameCollation) == name,
            cancellationToken);
        if (nameExists)
        {
            return Conflict(new { error = "A subcategory with that name already exists in this category." });
        }

        var subcategory = new Domain.Category.Subcategory { CategoryId = categoryId, Name = name };
        _db.Subcategories.Add(subcategory);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            new SubcategoryResponseDTO(subcategory.Id, subcategory.Name));
    }

    [HttpDelete("{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int categoryId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var category = await _db.Categories.SingleOrDefaultAsync(
            category => category.Id == categoryId && category.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        if (await _db.Subcategories.AnyAsync(item => item.CategoryId == categoryId, cancellationToken))
        {
            return Conflict(new { code = "category_has_subcategories" });
        }

        if (await _db.Movements.AnyAsync(item => item.CategoryId == categoryId, cancellationToken))
        {
            return Conflict(new { code = "category_has_movements" });
        }

        _db.Categories.Remove(category);
        return await SaveDeletionAsync(cancellationToken);
    }

    [HttpDelete("{categoryId:int}/subcategory/{subcategoryId:int}")]
    public async Task<IActionResult> DeleteSubcategory(
        int categoryId,
        int subcategoryId,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var subcategory = await _db.Subcategories.SingleOrDefaultAsync(
            item => item.Id == subcategoryId && item.CategoryId == categoryId &&
                _db.Categories.Any(category => category.Id == item.CategoryId && category.UserId == userId),
            cancellationToken);
        if (subcategory is null)
        {
            return NotFound();
        }

        if (await _db.Movements.AnyAsync(
            item => item.CategoryId == categoryId && item.SubcategoryId == subcategoryId,
            cancellationToken))
        {
            return Conflict(new { code = "subcategory_has_movements" });
        }

        _db.Subcategories.Remove(subcategory);
        return await SaveDeletionAsync(cancellationToken);
    }

    private async Task<IActionResult> SaveDeletionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            // A new reference can be inserted between validation and deletion.
            return Conflict(new { code = "item_has_dependencies" });
        }
    }

    private Task<bool> LockOwnerAsync(string userId, CancellationToken cancellationToken)
    {
        // Hold an update lock on the owner until the name check and insert are complete.
        return _db.Users
            .FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId}")
            .AnyAsync(cancellationToken);
    }
}
