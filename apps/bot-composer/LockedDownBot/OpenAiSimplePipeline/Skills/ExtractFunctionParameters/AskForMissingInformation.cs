using Newtonsoft.Json;
using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Skills.ExtractFunctionParameters;

public class AskForMissingInformation : IChainableCall<ExtractFunctionInformationOutput>
{
    private const string SystemPrompt = @"{systemPrompt}

The user wants to call the below function but is missing inputs for these parameters:

{missingParameters}

 Ask them friendly to provide information so we can call it.

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
            .Replace("{missingParameters}", string.Join("\n", _extractFunctionOutput.MissingParameters))
            .Replace("{function}", _function);

        var result = await client.PredictableOpenAiCall(
            systemPrompt,
            _userInput,
            token
        );

        return _extractFunctionOutput with { Complete = false, SuggestedPrompt = result };
    }
}
