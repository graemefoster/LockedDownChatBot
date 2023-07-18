using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Skills.SummariseContentSkill;

public record SummariseContentOutput(string Summary);

public class SummariseContent : IChainableCall<SummariseContentOutput>
{
    private readonly string _conversation;
    private readonly string? _systemPrompt;

    private const string SystemPrompt = @"{systemPrompt}
Please summarise the information entered. Do not make anything up.";

    public SummariseContent(string systemPrompt, string conversation)
    {
        _systemPrompt = systemPrompt;
        _conversation = conversation;
    }

    public async Task<SummariseContentOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var systemPrompt = SystemPrompt.
                Replace("{systemPrompt}", _systemPrompt);

        var result = await client.CreativeOpenAiCall(
            systemPrompt,
            _conversation,
            token
        );

        return new SummariseContentOutput(result);
    }
}