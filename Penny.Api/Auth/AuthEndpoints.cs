using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<IdentityUser> userManager)
    {
        var email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
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

        var result = await userManager.CreateAsync(user, request.Password);

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
    SignInManager<IdentityUser> signInManager)
    {
        var email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Enter an email and password." });
        }

        var result = await signInManager.PasswordSignInAsync(
            userName: email,
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

        return Results.Ok(new { user.Id, user.Email });
    }

    public sealed record LoginRequest(string? Email, string? Password);
}