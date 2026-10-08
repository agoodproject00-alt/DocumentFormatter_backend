using System.ComponentModel.DataAnnotations;

namespace DocumentFormatter.Application.Chats;

/// <summary>One persisted message in a document conversation.</summary>
public sealed record ChatMessageResponse(Guid Id, string Role, string Content, DateTimeOffset CreatedAtUtc);

/// <summary>Metadata for a document version stored with a conversation.</summary>
public sealed record ChatDocumentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Length,
    bool IsOriginal,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

/// <summary>A conversation with its messages and retained document versions.</summary>
public sealed record ChatThreadResponse(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ChatMessageResponse> Messages,
    IReadOnlyList<ChatDocumentResponse> Documents);

/// <summary>A compact item returned in the previous-chat list.</summary>
public sealed record ChatSummaryResponse(
    Guid Id,
    string Title,
    string? ActiveFileName,
    string? LastMessage,
    DateTimeOffset UpdatedAtUtc,
    int MessageCount);

/// <summary>Request body for sending an editing instruction to a conversation.</summary>
public sealed record SendChatMessageRequest
{
    [Required, StringLength(5000, MinimumLength = 1)]
    public required string Content { get; init; }
}

/// <summary>File bytes and metadata for downloading a retained document version.</summary>
public sealed record ChatDocumentDownload(string FileName, string ContentType, byte[] Content);

/// <summary>A predefined document formatting action available for selection.</summary>
public sealed record PredefinedActionResponse(int Id, string Label, string Prompt, bool IsUserOwned);

/// <summary>A page of predefined formatting actions and whether another page is available.</summary>
public sealed record PredefinedActionPageResponse(
    IReadOnlyList<PredefinedActionResponse> Items,
    bool HasMore);

/// <summary>Result of creating a shared predefined action or finding a matching label.</summary>
public sealed record SavePredefinedActionResponse(
    PredefinedActionResponse Action,
    bool Created);

/// <summary>User instruction used to generate a reusable formatting action.</summary>
public sealed record GeneratePredefinedActionRequest
{
    [Required, StringLength(5000, MinimumLength = 1)]
    [RegularExpression(@"[\s\S]*\S[\s\S]*", ErrorMessage = "Text must contain non-whitespace characters.")]
    public required string Text { get; init; }
}

public interface IPredefinedActionService
{
    Task<PredefinedActionPageResponse> ListAsync(
        string ownerUserId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<SavePredefinedActionResponse> GenerateAndCreateAsync(
        GeneratePredefinedActionRequest request,
        CancellationToken cancellationToken);
}

public interface IChatService
{
    Task<IReadOnlyList<ChatSummaryResponse>> ListAsync(string ownerUserId, CancellationToken cancellationToken);

    Task<ChatThreadResponse?> GetAsync(Guid chatId, string ownerUserId, CancellationToken cancellationToken);

    Task<ChatThreadResponse> CreateAsync(
        string ownerUserId,
        string fileName,
        string contentType,
        string extension,
        byte[] content,
        CancellationToken cancellationToken);

    Task<ChatThreadResponse?> SendAsync(
        Guid chatId,
        string ownerUserId,
        string instruction,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid chatId, string ownerUserId, CancellationToken cancellationToken);

    Task<ChatDocumentDownload?> GetDocumentAsync(
        Guid chatId,
        Guid documentId,
        string ownerUserId,
        CancellationToken cancellationToken);
}