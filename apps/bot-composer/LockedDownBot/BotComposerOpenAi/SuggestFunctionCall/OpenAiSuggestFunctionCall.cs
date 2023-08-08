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
public class OpenAiSuggestFunctionCall : Dialog
{
    private readonly SkillWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiSuggestFunctionCall(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SkillWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "OpenAiSuggestFunctionCall";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("function")] public StringExpression Function { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    [JsonProperty("inputs")] public ArrayExpression<string> Inputs { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"]);
        var prompt = SystemPrompt.GetValue(dc.State);
        var function = Function.GetValue(dc.State);
        var userInput = string.Join('\n', Inputs.GetValue(dc.State));

        var result = await 
            new ExtractInformationToCallFunction.FunctionWithPrompt()
                .ThenIf(
                    output => output.MissingParameters.Any(),
                    s => s.Resolve<GetMoreInputFromCustomerToCallInputFunction.FunctionWithPrompt>())
            .Run(
                    client, 
                    new ExtractInformationToCallFunction.Input(
                        prompt, 
                        userInput, 
                        JsonConvert.DeserializeObject<ExtractInformationToCallFunction.JsonSchemaFunctionInput>(function)!), 
                    cancellationToken);
        
        dc.State.SetValue(ResultProperty.GetValue(dc.State), result);
        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}