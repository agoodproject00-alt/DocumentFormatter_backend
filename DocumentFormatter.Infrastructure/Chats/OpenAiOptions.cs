namespace DocumentFormatter.Infrastructure.Chats;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://api.openai.com/v1";

    public string Model { get; set; } = "gpt-4.1";
}