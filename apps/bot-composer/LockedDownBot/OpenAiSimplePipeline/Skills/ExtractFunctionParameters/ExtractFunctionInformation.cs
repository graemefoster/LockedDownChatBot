using Newtonsoft.Json;
using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Skills.ExtractFunctionParameters;

public class ExtractFunctionInformation : IChainableCall<ExtractFunctionInformationOutput>
{
    private const string SystemPrompt = @"Read the users input and respond in JSON with arguments extracted from the user's input to call the function detailed below.

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
    
    private readonly string _prompt, _function, _userInput;

    public ExtractFunctionInformation(string prompt, string function, string userInput)
    {
        _prompt = prompt.Replace("\\n", "\n").ReplaceLineEndings("\n");
        _function = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(function), Formatting.Indented).ReplaceLineEndings("\n");
        _userInput = userInput;
    }
    
    public async Task<ExtractFunctionInformationOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var systemPrompt = SystemPrompt
            .Replace("{systemPrompt}", _prompt)
            .Replace("{function}", _function);

        var result = await client.PredictableOpenAiCall(
            systemPrompt,
            _userInput,
            token
        );
        
        var resultProperties = JsonConvert.DeserializeObject<OpenAiFunctionResponse>(result)!.Parameters;
        var resultParameters = resultProperties.Keys;

        //check for all expected parameters:
        var functionDefinition = JsonConvert.DeserializeObject<JsonSchemaFunctionInput>(_function)!;
        var parameters = functionDefinition.Parameters.Properties.Select(x => x.Key);
        var missingParameters = parameters.Where(x =>
                !resultParameters.Contains(x) ||
                resultProperties[x].ToString()!.Equals("UNKNOWN", StringComparison.InvariantCultureIgnoreCase))
            .ToHashSet();

        return new ExtractFunctionInformationOutput(!missingParameters.Any(), missingParameters, resultProperties, null);
    }

    record JsonSchemaFunctionInput(JsonSchemaFunctionInputParameters Parameters);
    record JsonSchemaFunctionInputParameters(Dictionary<string, object> Properties);
    record OpenAiFunctionResponse(Dictionary<string, object> Parameters);
}

public record ExtractFunctionInformationOutput(bool Complete, HashSet<string> MissingParameters, Dictionary<string, object> Response, string SuggestedPrompt);
