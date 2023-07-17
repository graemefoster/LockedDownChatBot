using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;
using OpenAiSimplePipeline.Skills.ExtractFunctionParameters;

namespace BotComposerOpenAi.SuggestFunctionCall;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiSuggestFunctionCall : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiSuggestFunctionCall(
        OpenAiClientFactory openAiClientFactory,
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new OpenAiClientFactory();
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
        var client = _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"], out var model);
        var prompt = SystemPrompt.GetValue(dc.State);
        var function = Function.GetValue(dc.State);
        var userInput = string.Join('\n', Inputs.GetValue(dc.State));

        var result = await 
            new ExtractFunctionInformation(prompt, function, userInput)
                .ThenIf(output => output.MissingParameters.Any(),
                    result => new AskForMissingInformation(prompt, function, userInput, result)
                )
            .Execute(client, cancellationToken);
        
        dc.State.SetValue(ResultProperty.GetValue(dc.State), result);
        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}