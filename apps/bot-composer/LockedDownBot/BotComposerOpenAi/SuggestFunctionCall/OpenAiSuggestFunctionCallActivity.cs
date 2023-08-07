using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.SuggestFunctionCall;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiSuggestFunctionCallActivity : Dialog
{
    private readonly SemanticKernelWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiSuggestFunctionCallActivity(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SemanticKernelWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "OpenAiSuggestFunctionCall";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("function")] public StringExpression Function { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var settings = (IDictionary<string, object>)dc.State["settings"];

        var client = _openAiClientFactory.GetFromSettings(settings);
        var memory = _openAiClientFactory.GetMemoryFromSettings(settings);

        var conversation = await dc.GetConversationForDialog(memory, cancellationToken);
        var prompt = SystemPrompt.GetValue(dc.State);
        var function = Function.GetValue(dc.State);

        var userInput = conversation.ToString();

        var result = await
            new ExtractInformationToCallFunction.Function()
                .ThenIf(
                    output => output.MissingParameters.Any(),
                    s => s.Resolve<GetMoreInputFromCustomerToCallInputFunction.Function>())
                .Run(
                    client,
                    new ExtractInformationToCallFunction.Input(
                        prompt,
                        userInput,
                        JsonConvert.DeserializeObject<ExtractInformationToCallFunction.JsonSchemaFunctionInput>(
                            function)!),
                    cancellationToken);

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