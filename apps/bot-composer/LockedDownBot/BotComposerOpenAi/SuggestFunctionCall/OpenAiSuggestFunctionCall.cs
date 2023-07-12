using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure.AI.OpenAI;
using BotComposerOpenAi.OpenAI;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.SuggestFunctionCall;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiSuggestFunctionCall : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    private const string SystemPromptInternal =
        @"Read the users input and respond in JSON with arguments extracted from the user's input to call the function detailed below.

- DO NOT show emotion.
- DO NOT invent parameters.
- Use ""UNKNOWN"" for arguments you don't know.
- ONLY respond in JSON.

{systemPrompt}

```function
{function}
```

```response
    {
        ""parameters"": {
            ""parameterName"": ""parameterValue""
        }
    }
```
";

    private const string MoreInformationPrompt = @"{systemPrompt}

The user wants to call the following function but is missing some parameters. Ask them nicely to provide the missing parameters.

```function
{function}
```
";

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
        var client = _openAiClientFactory.GetFromDialogueContext(dc, out var model);

        var input = string.Join('\n', Inputs.GetValue(dc.State));
        var result = await client.PredictableOpenAiCall(
            model,
            SystemPromptInternal
                .Replace("{systemPrompt}", SystemPrompt.GetValue(dc.State))
                .Replace("{function}", client.FormatJson(Function.GetValue(dc.State))),
            input, 
            cancellationToken: cancellationToken);

        var resultProperties = JsonConvert.DeserializeObject<OpenAiFunctionResponse>(result)!.Parameters;
        var resultParameters = resultProperties.Keys;
        var activityResponse = new SuggestFunctionCallResponse()
        {
            Response = resultProperties
        };

        //check for all expected parameters:
        var functionDefinition = JsonConvert.DeserializeObject<JsonSchemaFunctionInput>(Function.GetValue(dc.State))!;
        var parameters = functionDefinition.Parameters.Properties.Select(x => x.Key);
        var missingParameters = parameters.Any(x =>
            !resultParameters.Contains(x) ||
            resultProperties[x].ToString()!.Equals("UNKNOWN", StringComparison.InvariantCultureIgnoreCase));
        if (!missingParameters)
        {
            activityResponse.Complete = true;
        }
        else
        {
            result = await client.CreativeOpenAiCall(
                model,
                MoreInformationPrompt
                    .Replace("{systemPrompt}", SystemPrompt.GetValue(dc.State))
                    .Replace("{function}", client.FormatJson(Function.GetValue(dc.State))),
                input,
                cancellationToken);

            activityResponse.Complete = false;
            activityResponse.SuggestedPrompt = result;
        }

        dc.State.SetValue(ResultProperty.GetValue(dc.State), activityResponse);
        return await dc.EndDialogAsync(result: activityResponse, cancellationToken);
    }
}