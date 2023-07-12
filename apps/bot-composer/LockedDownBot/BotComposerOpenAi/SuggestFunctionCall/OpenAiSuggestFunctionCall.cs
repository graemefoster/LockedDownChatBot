using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.SuggestFunctionCall;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiSuggestFunctionCall : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    private const string SystemPromptInternal = @"Read the users input and respond in JSON arguments extracted from the users's input, to call the function detailed below.

- DO NOT invent parameters.
- Use ""UNKNOWN"" for arguments you don't know.
- ONLY respond in JSON

{systemPrompt}

```function
{function}
```

```response
    {
        parameters: {
            ""parameterName"": ""parameterValue""
        }
    }
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

    [JsonProperty("inputs")]
    public ArrayExpression<string> Inputs { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromDialogueContext(dc, out var model);

        var input = string.Join('\n', Inputs.GetValue(dc.State));

        var gptFriendlyFunctionInput = JsonConvert.SerializeObject(
            JsonConvert.DeserializeObject(Function.GetValue(dc.State)),
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            });

        var response = await client.GetChatCompletionsAsync(
            model,
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(
                        ChatRole.System,
                        SystemPromptInternal
                            .Replace("{systemPrompt}", SystemPrompt.GetValue(dc.State))
                            .Replace(
                                "{function}",
                                gptFriendlyFunctionInput //affects the output format...
                            )
                            .Replace("\\n", "\n")
                            .ReplaceLineEndings("\n")
                        ),
                    new ChatMessage(
                        ChatRole.User, input)
                },
                Temperature = 0,
                PresencePenalty = 0,
                FrequencyPenalty = 0,
                NucleusSamplingFactor = 0,
                MaxTokens = 800
            }, cancellationToken);

        var result = response;
        Dictionary<string, object> resultProperties;
        try
        {
            resultProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
        }
        catch (Exception)
        {
            resultProperties = JsonConvert.DeserializeObject<OpenAiFunctionResponse>(result)!.Parameters;
        }

        var resultParameters = resultProperties.Keys;
        var activityResponse = new SuggestFunctionCallResponse()
        {
            Response = resultProperties
        };

        //check for all expected parameters:
        var functionDefinition = JsonConvert.DeserializeObject<JsonSchemaFunctionInput>(Function.GetValue(dc.State))!;
        var parameters = functionDefinition.Parameters.Properties.Select(x => x.Key);
        var missingParameters = parameters.Any(x =>
            !resultParameters.Contains(x) || resultProperties[x]
                .ToString().Equals("UNKNOWN", StringComparison.InvariantCultureIgnoreCase));
        if (!missingParameters)
        {
            activityResponse.Complete = true;
        }
        else
        {
            response = await client.GetChatCompletionsAsync(
                model,
                new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatMessage(
                            ChatRole.System,
                            SystemPromptInternal
                                .Replace("{systemPrompt}",
                                    "The user wants to call the following function but is missing parameters. Get them to provide the missing parameters.")
                                .Replace("{function}", gptFriendlyFunctionInput)
                                .Replace("\\n", "\n")),
                        new ChatMessage(
                            ChatRole.User, input)
                    },
                    Temperature = 0,
                    PresencePenalty = 0
                }, cancellationToken);

            activityResponse.Complete = false;
            activityResponse.SuggestedPrompt = response;
        }

        dc.State.SetValue(ResultProperty.GetValue(dc.State), activityResponse);
        return await dc.EndDialogAsync(result: activityResponse, cancellationToken);
    }
}

public class SuggestFunctionCallResponse
{
    public bool Complete { get; set; }
    public string SuggestedPrompt { get; set; }
    public object Response { get; set; }
}

public class OpenAiFunctionResponse
{
    public Dictionary<string, object> Parameters { get; set; }
}

public class JsonSchemaFunctionInput
{
    public string Name { get; set; }
    public JsonSchemaFunctionInputParameters Parameters { get; set; }
}

public class JsonSchemaFunctionInputParameters
{
    public Dictionary<string, object> Properties { get; set; }
}
