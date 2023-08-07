using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using LockedDownBotSemanticKernel.Primitives;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.UpdateGptCoversation;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class UpdateGptConversationActivity : Dialog
{
    private readonly SemanticKernelWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public UpdateGptConversationActivity(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SemanticKernelWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "UpdateGptConversation";

    [JsonProperty("input")] public StringExpression Input { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var settings = (IDictionary<string, object>)dc.State["settings"];
        var memory = _openAiClientFactory.GetMemoryFromSettings(settings);
        var conversation = await dc.GetConversationForDialog(memory, cancellationToken);
        conversation.UpdateConversationWithUserInput(Input.GetValue(dc.State));
        await memory.SaveConversation(conversation, cancellationToken);
        return await dc.EndDialogAsync(null, cancellationToken);
    }
}