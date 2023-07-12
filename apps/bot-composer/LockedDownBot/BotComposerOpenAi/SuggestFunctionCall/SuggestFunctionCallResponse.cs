namespace BotComposerOpenAi.SuggestFunctionCall;

public class SuggestFunctionCallResponse
{
    public bool Complete { get; set; }
    public string SuggestedPrompt { get; set; }
    public object Response { get; set; }
}