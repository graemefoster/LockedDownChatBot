using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Prompts.ExtractIntent;

public record ExtractIntentOutput(bool Unknown, string? Intent, string? SuggestedPrompt);

public class ExtractIntentFromInput : IChainableCall<ExtractIntentOutput>
{
    private const string SystemPrompt = @"{systemPrompt}

Find the INTENT of the user's input. 
Possible intents are:

{intents}

Use ""Unknown"" if the intent is not in this list.
Respond with the intent as a single word."; 
    
    private const string GetMoreInfoPrompt = @"{systemPrompt}

You need to find the user's intent. Possible intents are 

{intents}

Ask them what they want to do.
";
    
    private readonly string _prompt;
    private readonly string _userInput;
    private readonly string[] _intents;

    public ExtractIntentFromInput(string prompt, string[] intents, string userInput)
    {
        _prompt = prompt.Replace("\\n", "\n").ReplaceLineEndings("\n");
        _intents = intents;
        _userInput = userInput;
    }

    public async Task<ExtractIntentOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var systemPrompt = SystemPrompt
            .Replace("{systemPrompt}", _prompt)
            .Replace("{intents}", string.Join('\n', _intents));

        var result = await client.PredictableOpenAiCall(
            systemPrompt,
            _userInput,
            token
        );

        
        var foundIntent = _intents.Contains(result);

        if (foundIntent)
        {
            return new ExtractIntentOutput(false, result, null);
        }

        systemPrompt = GetMoreInfoPrompt
            .Replace("{systemPrompt}", _prompt)
            .Replace($"{_intents}", string.Join('\n', _intents));

        result = await client.PredictableOpenAiCall(
            systemPrompt,
            _userInput,
            token
        );

        return new ExtractIntentOutput(true, null, result);
    }
}
