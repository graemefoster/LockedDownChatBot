namespace BotComposerOpenAi.TryToFindUserIntent;

public class IntentResult
{
    public bool Unknown { get; set; }
    public string Intent { get; set; }
    public string SuggestedPrompt { get; set; }
}