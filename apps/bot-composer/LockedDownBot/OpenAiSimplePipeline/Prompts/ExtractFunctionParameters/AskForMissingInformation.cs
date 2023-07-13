using Newtonsoft.Json;
using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Prompts.ExtractFunctionParameters;

public class AskForMissingInformation : IChainableCall<ExtractFunctionInformationOutput>
{
    private const string SystemPrompt = @"{systemPrompt}

The user wants to call the following function but is missing some parameters. Ask them nicely to provide the missing parameters.

```function
{function}
```
"; 
    
    private readonly string _prompt;
    private readonly string _function;
    private readonly string _userInput;
    private readonly ExtractFunctionInformationOutput _extractFunctionOutput;

    public AskForMissingInformation(string prompt, string function, string userInput, ExtractFunctionInformationOutput extractFunctionOutput)
    {
        _prompt = prompt.Replace("\\n", "\n").ReplaceLineEndings("\n");
        _function = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(function), Formatting.Indented).ReplaceLineEndings("\n");
        _userInput = userInput;
        _extractFunctionOutput = extractFunctionOutput;
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

        return _extractFunctionOutput with { Complete = false, SuggestedPrompt = result };
    }
}
