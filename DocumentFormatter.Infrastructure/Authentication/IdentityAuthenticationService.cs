using DocumentFormatter.Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace DocumentFormatter.Infrastructure.Authentication;

public sealed class IdentityAuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IAuthenticationService
{
    public async Task<AuthenticationResult> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return AuthenticationResult.Failure(result.Errors.Select(error => error.Description));
        }

        var roleResult = await userManager.AddToRoleAsync(user, "User");
        if (roleResult.Succeeded)
        {
            return AuthenticationResult.Success();
        }

        await userManager.DeleteAsync(user);
        return AuthenticationResult.Failure(roleResult.Errors.Select(error => error.Description));
    }

    public async Task<AuthenticationResult> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && !(await userManager.GetRolesAsync(user)).Any())
        {
            await userManager.AddToRoleAsync(user, "User");
        }

        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        return result.Succeeded
            ? AuthenticationResult.Success()
            : AuthenticationResult.Failure(["Invalid email or password."]);
    }

    public Task LogoutAsync() => signInManager.SignOutAsync();
}