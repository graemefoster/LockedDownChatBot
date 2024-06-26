using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseAsk;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.SummariseConversation;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class SummariseConversationActivity : Dialog
{
    private readonly SkillWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public SummariseConversationActivity(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SkillWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "SummariseConversation";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object? options = null,
        CancellationToken cancellationToken = new())
    {
        var settings = (IDictionary<string, object>)dc.State["settings"];

        var client = _openAiClientFactory.GetFromSettings(settings);
        var memory = _openAiClientFactory.GetMemoryFromSettings(settings);

        var conversation = await dc.GetConversationForDialog(memory, cancellationToken);

        var prompt = SystemPrompt.GetValue(dc.State);
        
        var input = conversation.ToString();

        var response = await
            new SummariseAskFunction.Function()
                .Run(client, new SummariseAskFunction.Input(prompt, input), cancellationToken);

        if (ResultProperty != null)
        {
            dc.State.SetValue(ResultProperty.GetValue(dc.State), response.Summarisation);
        }

        return await dc.EndDialogAsync(result: response, cancellationToken);
    }
}