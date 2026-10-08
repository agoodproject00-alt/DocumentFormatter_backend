using System.Security.Claims;
using DocumentFormatter.Application.Chats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFormatter.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/predefined-actions")]
public sealed class PredefinedActionsController(IPredefinedActionService actions) : ControllerBase
{
    private const int ActionPageSize = 4;

    [HttpGet]
    [EndpointSummary("Search predefined formatting actions")]
    [EndpointDescription("Returns up to four active actions ranked by relevance, with page-based results.")]
    [ProducesResponseType(typeof(PredefinedActionPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PredefinedActionPageResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (search?.Length > 200)
        {
            return BadRequest("Search text must not exceed 200 characters.");
        }

        if (page < 1 || page > 1_000_000)
        {
            return BadRequest("Page must be between 1 and 1,000,000.");
        }

        return Ok(await actions.ListAsync(CurrentUserId(), search, page, ActionPageSize, cancellationToken));
    }

    [HttpPost]
    [EndpointSummary("Generate or reuse a shared formatting action")]
    [EndpointDescription("Generates a reusable action from user instructions. If a shared action with the generated label already exists, returns it without inserting another row.")]
    [ProducesResponseType(typeof(SavePredefinedActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SavePredefinedActionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavePredefinedActionResponse>> Create(
        GeneratePredefinedActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await actions.GenerateAndCreateAsync(request, cancellationToken);
        return result.Created
            ? CreatedAtAction(nameof(List), null, result)
            : Ok(result);
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated user id is missing.");
}
