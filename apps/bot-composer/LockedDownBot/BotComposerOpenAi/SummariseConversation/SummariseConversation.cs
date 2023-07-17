using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.Skills.SummariseInputSkill;

namespace BotComposerOpenAi.SummariseConversation;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class SummariseConversation : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    [JsonConstructor]
    public SummariseConversation(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new OpenAiClientFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "SummariseConversation";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("conversation")] public ArrayExpression<string> Conversation { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"], out var model);

        var prompt = SystemPrompt.GetValue(dc.State);
        
        //read the conversation so-far
        var input = string.Join('\n', Conversation.GetValue(dc.State));

        var response = await
            new SummariseCurrentInformation(prompt, input)
                .Execute(client, cancellationToken);

        dc.State.SetValue(ResultProperty.GetValue(dc.State), response.Summary);
        return await dc.EndDialogAsync(result: response, cancellationToken);
    }
}