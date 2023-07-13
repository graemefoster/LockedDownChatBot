using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.Prompts.ExtractIntent;

namespace BotComposerOpenAi.TryToFindUserIntent;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiDetectIntent : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiDetectIntent(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new OpenAiClientFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "OpenAiDetectIntent";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("intents")] public ArrayExpression<string> Intents { get; set; }

    [JsonProperty("inputs")] public ArrayExpression<string> Inputs { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"], out var model);

        var prompt = SystemPrompt.GetValue(dc.State);
        var input = string.Join('\n', Inputs.GetValue(dc.State));
        var intents = Intents.GetValue(dc.State)?.ToArray() ?? Array.Empty<string>();

        var response = await
            new ExtractIntentFromInput(prompt, intents, input)
                .Execute(client, cancellationToken);

        dc.State.SetValue(ResultProperty.GetValue(dc.State), response);
        return await dc.EndDialogAsync(result: response, cancellationToken);
    }
}