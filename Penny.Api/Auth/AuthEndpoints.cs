using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Penny.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/register", RegisterAsync)
            .WithName("RegisterUser")
            .WithOpenApi();

        app.MapPost("/auth/login", LoginAsync)
            .WithName("LoginUser")
            .WithOpenApi();

        app.MapPost("/auth/logout", LogoutAsync)
            .RequireAuthorization()
            .WithName("LogoutUser")
            .WithOpenApi();

        app.MapGet("/auth/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithOpenApi();

        app.MapPut("/auth/me", UpdateProfileAsync)
            .RequireAuthorization()
            .WithName("UpdateProfile")
            .WithOpenApi();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<IdentityUser> userManager)
    {
        var email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            return Results.BadRequest(new { error = "Enter a valid email address." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Enter a password." });
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email
        };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException exception) when (IsDuplicateIdentity(exception))
        {
            return Results.BadRequest(new { error = "The email or username is already in use." });
        }

        if (!result.Succeeded)
        {
            return Results.BadRequest(new
            {
                errors = result.Errors.Select(error => error.Description)
            });
        }

        return Results.StatusCode(StatusCodes.Status201Created);
    }

    public sealed record RegisterRequest(string? Email, string? Password);

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        var email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Enter an email and password." });
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return Results.Unauthorized();

        var result = await signInManager.PasswordSignInAsync(
            user: user,
            password: request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        return result.Succeeded
            ? Results.NoContent()
            : Results.Unauthorized();
    }

    private static async Task<IResult> LogoutAsync(
    SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
    ClaimsPrincipal principal,
    UserManager<IdentityUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        return ProfileResponse(user);
    }

    private static async Task<IResult> UpdateProfileAsync(
        ProfileRequest request,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return Results.Unauthorized();

        var userName = request.UserName?.Trim();
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(userName) || userName.Length > 256)
            return ProfileError("InvalidUserName");
        if (string.IsNullOrEmpty(email) || email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            return ProfileError("InvalidEmail");
        if (string.IsNullOrEmpty(request.CurrentPassword))
            return ProfileError("PasswordMismatch");

        var verification = await signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: true);
        if (verification.IsLockedOut)
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        if (!verification.Succeeded)
            return ProfileError("PasswordMismatch");

        user.UserName = userName;
        if (!string.Equals(user.Email, email, StringComparison.Ordinal))
        {
            user.Email = email;
            user.EmailConfirmed = false;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        IdentityResult result;
        try
        {
            result = string.IsNullOrEmpty(request.NewPassword)
                ? await userManager.UpdateAsync(user)
                : await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        catch (DbUpdateException exception) when (IsDuplicateIdentity(exception))
        {
            return Results.Conflict(new { codes = new[] { "DuplicateProfile" } });
        }

        if (!result.Succeeded)
            return Results.BadRequest(new { codes = result.Errors.Select(error => error.Code) });

        await signInManager.RefreshSignInAsync(user);
        return ProfileResponse(user);
    }

    private static IResult ProfileError(string code) =>
        Results.BadRequest(new { codes = new[] { code } });

    private static bool IsDuplicateIdentity(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static IResult ProfileResponse(IdentityUser user) =>
        Results.Ok(new { user.Id, user.UserName, user.Email });

    public sealed record ProfileRequest(string? UserName, string? Email, string? CurrentPassword, string? NewPassword);

    public sealed record LoginRequest(string? Email, string? Password);
}
