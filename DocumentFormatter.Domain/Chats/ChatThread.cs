namespace DocumentFormatter.Domain.Chats;

public sealed class ChatThread
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string OwnerUserId { get; set; }

    public required string Title { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<ChatMessage> Messages { get; set; } = [];

    public List<ChatDocument> Documents { get; set; } = [];
}