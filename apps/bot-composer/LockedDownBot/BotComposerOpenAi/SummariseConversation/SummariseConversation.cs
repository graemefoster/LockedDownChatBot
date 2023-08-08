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
public class SummariseConversation : Dialog
{
    private readonly SkillWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public SummariseConversation(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SkillWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "SummariseConversation";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("conversation")] public ArrayExpression<string> Conversation { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var client =
            _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"]);

        var prompt = SystemPrompt.GetValue(dc.State);

        //read the conversation so-far
        var input = string.Join('\n', Conversation.GetValue(dc.State));

        var response = await
            new SummariseAskFunction.Function()
                .Run(client, new SummariseAskFunction.Input(prompt, input), cancellationToken);

        dc.State.SetValue(ResultProperty.GetValue(dc.State), response.Summarisation);
        return await dc.EndDialogAsync(result: response, cancellationToken);
    }
}