namespace DocumentFormatter.Domain.Chats;

public sealed class PredefinedAction
{
    public int Id { get; set; }

    public required string Label { get; set; }

    public required string Prompt { get; set; }

    public string? OwnerUserId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
