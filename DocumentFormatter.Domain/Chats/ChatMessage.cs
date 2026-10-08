namespace DocumentFormatter.Domain.Chats;

public sealed class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChatThreadId { get; set; }

    public required string Role { get; set; }

    public required string Content { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}