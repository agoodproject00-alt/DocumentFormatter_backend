namespace DocumentFormatter.Domain.Chats;

public sealed class ChatDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChatThreadId { get; set; }

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public required string Extension { get; set; }

    public required byte[] Content { get; set; }

    public long Length { get; set; }

    public bool IsOriginal { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}