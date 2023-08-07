using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using LockedDownBotSemanticKernel.Memory;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntentNextResponse;
using Microsoft.Azure.Cosmos;
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

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var settings = (IDictionary<string, object>)dc.State["settings"];

        var client = _openAiClientFactory.GetFromSettings(settings);
        var memory = _openAiClientFactory.GetMemoryFromSettings(settings);

        var conversation = await dc.GetConversationForDialog(memory, cancellationToken);
        conversation.UpdateConversationWithUserInput(dc.Context.Activity.Text);

        var prompt = SystemPrompt.GetValue(dc.State);
        var input = conversation.ToString();
        var intents = Intents.GetValue(dc.State)?.ToArray() ?? Array.Empty<string>();

        var result = await
            new ExtractIntentFromInputFunction.Function()
                .ThenIf(x => !x.FoundIntent,
                    s => s.Resolve<GetMoreInputFromCustomerToDetectIntentInputFunction>())
                .Run(client, new ExtractIntentFromInputFunction.Input(prompt, intents, input), cancellationToken);

        if (result.NextRecommendation != null)
        {
            conversation.UpdateConversationWithSystemResponse(result.NextRecommendation);
        }

        await memory.SaveConversation(conversation, cancellationToken);

        if (ResultProperty != null)
        {
            dc.State.SetValue(ResultProperty.GetValue(dc.State), result);
        }

        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}