using DocumentFormatter.Application.Chats;
using DocumentFormatter.Domain.Chats;
using DocumentFormatter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentFormatter.Infrastructure.Chats;

public sealed class DocumentChatService(
    AuthDbContext database,
    OpenAiDocumentEditor documentEditor,
    ILogger<DocumentChatService> logger) : IChatService
{
    private const string AssistantRole = "assistant";
    private const string UserRole = "user";

    public async Task<IReadOnlyList<ChatSummaryResponse>> ListAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        return await database.ChatThreads
            .AsNoTracking()
            .Where(chat => chat.OwnerUserId == ownerUserId)
            .OrderByDescending(chat => chat.UpdatedAtUtc)
            .Select(chat => new ChatSummaryResponse(
                chat.Id,
                chat.Title,
                chat.Documents.Where(document => document.IsActive)
                    .Select(document => document.FileName).FirstOrDefault(),
                chat.Messages.OrderByDescending(message => message.CreatedAtUtc)
                    .Select(message => message.Content).FirstOrDefault(),
                chat.UpdatedAtUtc,
                chat.Messages.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatThreadResponse?> GetAsync(
        Guid chatId,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        var chat = await database.ChatThreads
            .AsNoTracking()
            .SingleOrDefaultAsync(chat => chat.Id == chatId && chat.OwnerUserId == ownerUserId, cancellationToken);
        return chat is null ? null : await BuildResponseAsync(chat, cancellationToken);
    }

    public async Task<ChatThreadResponse> CreateAsync(
        string ownerUserId,
        string fileName,
        string contentType,
        string extension,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        var title = Path.GetFileNameWithoutExtension(safeFileName);
        var now = DateTimeOffset.UtcNow;
        var chat = new ChatThread
        {
            OwnerUserId = ownerUserId,
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled document" : title[..Math.Min(title.Length, 200)],
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Documents =
            [
                new ChatDocument
                {
                    FileName = safeFileName,
                    ContentType = contentType,
                    Extension = extension,
                    Content = content,
                    Length = content.LongLength,
                    IsOriginal = true,
                    IsActive = true,
                    CreatedAtUtc = now
                }
            ],
            Messages =
            [
                new ChatMessage
                {
                    Role = AssistantRole,
                    Content = $"{safeFileName} is attached. Tell me what you would like changed, and I will return an edited copy.",
                    CreatedAtUtc = now
                }
            ]
        };

        database.ChatThreads.Add(chat);
        await database.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(chat, cancellationToken);
    }

    public async Task<ChatThreadResponse?> SendAsync(
        Guid chatId,
        string ownerUserId,
        string instruction,
        CancellationToken cancellationToken)
    {
        var chat = await database.ChatThreads
            .SingleOrDefaultAsync(chat => chat.Id == chatId && chat.OwnerUserId == ownerUserId, cancellationToken);
        if (chat is null)
        {
            return null;
        }

        var activeDocument = await database.ChatDocuments
            .SingleOrDefaultAsync(document => document.ChatThreadId == chatId && document.IsActive, cancellationToken);
        if (activeDocument is null)
        {
            throw new InvalidOperationException("This conversation no longer has an active document.");
        }

        var now = DateTimeOffset.UtcNow;
        database.ChatMessages.Add(new ChatMessage
        {
            ChatThreadId = chatId,
            Role = UserRole,
            Content = instruction.Trim(),
            CreatedAtUtc = now
        });
        chat.UpdatedAtUtc = now;
        await database.SaveChangesAsync(cancellationToken);

        try
        {
            var edited = await documentEditor.EditAsync(
                activeDocument.Content,
                activeDocument.Extension,
                instruction.Trim(),
                cancellationToken);

            activeDocument.IsActive = false;
            database.ChatDocuments.Add(new ChatDocument
            {
                ChatThreadId = chatId,
                FileName = edited.FileName,
                ContentType = activeDocument.ContentType,
                Extension = activeDocument.Extension,
                Content = edited.Content,
                Length = edited.Content.LongLength,
                IsOriginal = false,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            database.ChatMessages.Add(new ChatMessage
            {
                ChatThreadId = chatId,
                Role = AssistantRole,
                Content = string.IsNullOrWhiteSpace(edited.Summary)
                    ? $"I edited the document and saved the updated copy as {edited.FileName}."
                    : $"Updated document: {edited.FileName}\n\n{edited.Summary}",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Document edit failed for chat {ChatId} ({ErrorType}).", chatId, exception.GetType().Name);
            database.ChatMessages.Add(new ChatMessage
            {
                ChatThreadId = chatId,
                Role = AssistantRole,
                Content = exception is InvalidOperationException
                    ? exception.Message
                    : "The document editing service is temporarily unavailable. Your message has been saved; please try again shortly.",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        chat.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(chat, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid chatId,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        var chat = await database.ChatThreads
            .SingleOrDefaultAsync(chat => chat.Id == chatId && chat.OwnerUserId == ownerUserId, cancellationToken);
        if (chat is null)
        {
            return false;
        }

        database.ChatThreads.Remove(chat);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ChatDocumentDownload?> GetDocumentAsync(
        Guid chatId,
        Guid documentId,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        return await database.ChatDocuments
            .AsNoTracking()
            .Where(document => document.Id == documentId
                && document.ChatThreadId == chatId
                && database.ChatThreads.Any(chat => chat.Id == chatId && chat.OwnerUserId == ownerUserId))
            .Select(document => new ChatDocumentDownload(document.FileName, document.ContentType, document.Content))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<ChatThreadResponse> BuildResponseAsync(ChatThread chat, CancellationToken cancellationToken)
    {
        var messages = await database.ChatMessages
            .AsNoTracking()
            .Where(message => message.ChatThreadId == chat.Id)
            .OrderBy(message => message.CreatedAtUtc)
            .Select(message => new ChatMessageResponse(message.Id, message.Role, message.Content, message.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var documents = await database.ChatDocuments
            .AsNoTracking()
            .Where(document => document.ChatThreadId == chat.Id)
            .OrderBy(document => document.CreatedAtUtc)
            .Select(document => new ChatDocumentResponse(
                document.Id,
                document.FileName,
                document.ContentType,
                document.Length,
                document.IsOriginal,
                document.IsActive,
                document.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new ChatThreadResponse(chat.Id, chat.Title, chat.CreatedAtUtc, chat.UpdatedAtUtc, messages, documents);
    }
}