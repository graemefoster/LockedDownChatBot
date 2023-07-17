using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Skills.SummariseInputSkill;

public record SummariseInputOutput(string Summary);

public class SummariseCurrentInformation : IChainableCall<SummariseInputOutput>
{
    private readonly string _conversation;
    private readonly string? _systemPrompt;

    private const string SystemPrompt = @"{systemPrompt}
Read the conversation so-far, and summarise what the the user wants to achieve in a single sentence.
";

    public SummariseCurrentInformation(string systemPrompt, string conversation)
    {
        _systemPrompt = systemPrompt;
        _conversation = conversation;
    }

    public async Task<SummariseInputOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var systemPrompt = SystemPrompt.
                Replace("{systemPrompt}", _systemPrompt);

        var result = await client.CreativeOpenAiCall(
            systemPrompt,
            _conversation,
            token
        );

        return new SummariseInputOutput(result);
    }
}