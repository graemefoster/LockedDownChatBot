namespace LockedDownBotSemanticKernel.Memory;

public class Chat
{
    public string Actor { get; set; }
    public string Message { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}