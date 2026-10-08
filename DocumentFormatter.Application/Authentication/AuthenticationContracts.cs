using System.ComponentModel.DataAnnotations;

namespace DocumentFormatter.Application.Authentication;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(12)]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(12)]
    public string Password { get; init; } = string.Empty;
}

public sealed record AuthenticationResult(bool Succeeded, IReadOnlyCollection<string> Errors)
{
    public static AuthenticationResult Success() => new(true, Array.Empty<string>());

    public static AuthenticationResult Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request);

    Task<AuthenticationResult> LoginAsync(LoginRequest request);

    Task LogoutAsync();
}