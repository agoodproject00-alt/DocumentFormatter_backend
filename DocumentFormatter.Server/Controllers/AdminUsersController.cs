using DocumentFormatter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentFormatter.Server.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController(AuthDbContext database) : ControllerBase
{
    [HttpGet("recent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Recent([FromQuery] int limit = 8, CancellationToken cancellationToken = default)
    {
        var users = await database.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAtUtc)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(user => user.Id).ToArray();
        var userRoles = await database.UserRoles
            .Where(userRole => userIds.Contains(userRole.UserId))
            .Join(database.Roles, userRole => userRole.RoleId, role => role.Id,
                (userRole, role) => new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        return Ok(users.Select(user => new
        {
            user.Id,
            user.Email,
            user.CreatedAtUtc,
            Roles = userRoles.Where(userRole => userRole.UserId == user.Id).Select(userRole => userRole.Name)
        }));
    }
}