namespace LockedDownBotSemanticKernel.Memory;

public class Chat
{
    public string Actor { get; init; } = default!;
    public string Message { get; init; } = default!;
    public DateTimeOffset Timestamp { get; set; }
}
