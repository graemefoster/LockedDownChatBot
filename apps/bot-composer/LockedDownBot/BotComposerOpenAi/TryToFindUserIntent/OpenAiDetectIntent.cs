using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntentNextResponse;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.TryToFindUserIntent;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiDetectIntent : Dialog
{
    private readonly SemanticKernelWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiDetectIntent(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SemanticKernelWrapperFactory();
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
        var client =
            _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"]);

        var prompt = SystemPrompt.GetValue(dc.State);
        var input = string.Join(Environment.NewLine, Inputs.GetValue(dc.State));
        var intents = Intents.GetValue(dc.State)?.ToArray() ?? Array.Empty<string>();

        var result = await
            new ExtractIntentFromInputFunction.Function()
                .ThenIf(x => !x.FoundIntent,
                    s => s.Resolve<GetMoreInputFromCustomerToDetectIntentInputFunction>())
                .Execute(client, new ExtractIntentFromInputFunction.Input(prompt, intents, input), cancellationToken);

        dc.State.SetValue(ResultProperty.GetValue(dc.State), result);
        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}