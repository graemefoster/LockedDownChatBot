using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure.AI.OpenAI;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Foundational.ChitChat;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.OpenAiResponseWithSystemPrompt;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiResponseWithSystemPrompt : Dialog
{
    private readonly SemanticKernelWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiResponseWithSystemPrompt(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SemanticKernelWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "OpenAiResponseWithSystemPrompt";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("inputs")] public ArrayExpression<string> Inputs { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"]);
        var rawClient =
            _openAiClientFactory.GetRawClientFromSettings((IDictionary<string, object>)dc.State["settings"],
                out var model);
        var input = string.Join('\n', Inputs.GetValue(dc.State));

        var response = await new ChitChatFunction.Function(rawClient, model)
            .Run(client, new ChitChatFunction.Input(
                new[]
                {
                    new ChatMessage(ChatRole.System, SystemPrompt.GetValue(dc.State)),
                    new ChatMessage(ChatRole.User, input)
                }), cancellationToken);

        var result = response;

        if (this.ResultProperty != null)
        {
            dc.State.SetValue(this.ResultProperty.GetValue(dc.State), result);
        }

        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}