using Microsoft.AspNetCore.Identity;

namespace DocumentFormatter.Infrastructure.Authentication;

public sealed class ApplicationUser : IdentityUser
{
	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}