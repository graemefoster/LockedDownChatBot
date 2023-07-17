using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Skills.ExtractSearchTerms;

public record ExtractSearchTermsOutput(string? SuggestedTerms);

public class ExtractSearchTermsFromInput : IChainableCall<ExtractSearchTermsOutput>
{
    private const string SystemPrompt = @"{systemPrompt}
Given the conversation, what search terms should we use to find the answer?";

    private readonly string _prompt;
    private readonly string _userInput;

    public ExtractSearchTermsFromInput(string prompt, string userInput)
    {
        _prompt = prompt.Replace("\\n", "\n").ReplaceLineEndings("\n");
        _userInput = userInput;
    }

    public async Task<ExtractSearchTermsOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var systemPrompt = SystemPrompt
            .Replace("{systemPrompt}", _prompt);

        var result = await client.PredictableOpenAiCall(
            systemPrompt,
            _userInput,
            token
        );

        return new ExtractSearchTermsOutput(result);
    }
}