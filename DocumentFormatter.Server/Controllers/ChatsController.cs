using System.Security.Claims;
using DocumentFormatter.Application.Chats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFormatter.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/chats")]
public sealed class ChatsController(IChatService chats) : ControllerBase
{
    private const long MaxDocumentSize = 25 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> SupportedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ChatSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ChatSummaryResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await chats.ListAsync(CurrentUserId(), cancellationToken));
    }

    [HttpGet("{chatId:guid}")]
    [ProducesResponseType(typeof(ChatThreadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatThreadResponse>> Get(Guid chatId, CancellationToken cancellationToken)
    {
        var chat = await chats.GetAsync(chatId, CurrentUserId(), cancellationToken);
        return chat is null ? NotFound() : Ok(chat);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ChatThreadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatThreadResponse>> Create(
        [FromForm] IFormFile? document,
        CancellationToken cancellationToken)
    {
        if (document is null || document.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "A document is required." });
        }

        if (document.Length > MaxDocumentSize)
        {
            return BadRequest(new ProblemDetails { Title = "Documents must be 25 MB or smaller." });
        }

        var extension = Path.GetExtension(document.FileName);
        if (!SupportedTypes.TryGetValue(extension, out var contentType))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Choose a PDF, DOCX, or PPTX document.");
        }

        var fileName = Path.GetFileName(document.FileName);
        if (fileName.Length > 255)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The document filename must be 255 characters or fewer.");
        }

        await using var memory = new MemoryStream((int)document.Length);
        await document.CopyToAsync(memory, cancellationToken);
        var chat = await chats.CreateAsync(
            CurrentUserId(),
            fileName,
            contentType,
            extension.ToLowerInvariant(),
            memory.ToArray(),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { chatId = chat.Id }, chat);
    }

    [HttpPost("{chatId:guid}/messages")]
    [ProducesResponseType(typeof(ChatThreadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatThreadResponse>> Send(
        Guid chatId,
        SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var chat = await chats.SendAsync(chatId, CurrentUserId(), request.Content, cancellationToken);
        return chat is null ? NotFound() : Ok(chat);
    }

    [HttpDelete("{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid chatId, CancellationToken cancellationToken)
    {
        var deleted = await chats.DeleteAsync(chatId, CurrentUserId(), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{chatId:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(
        Guid chatId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await chats.GetDocumentAsync(chatId, documentId, CurrentUserId(), cancellationToken);
        return document is null
            ? NotFound()
            : File(document.Content, document.ContentType, document.FileName);
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated user id is missing.");
}