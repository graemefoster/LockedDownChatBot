using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Prompts.SummariseCurrentConversation;

public record SummariseCurrentInformationOutput(string Summary);

public class SummariseCurrentInformation : IChainableCall<SummariseCurrentInformationOutput>
{
    private readonly string _conversation;
    private readonly string? _systemPrompt;

    private const string SystemPrompt = @"{systemPrompt}
Read the conversation so-far, and please summarise the User's ask into a single sentence.
";

    public SummariseCurrentInformation(string systemPrompt, string conversation)
    {
        _systemPrompt = systemPrompt;
        _conversation = conversation;
    }

    public async Task<SummariseCurrentInformationOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var systemPrompt = SystemPrompt.
                Replace("{systemPrompt}", _systemPrompt);

        var result = await client.CreativeOpenAiCall(
            systemPrompt,
            _conversation,
            token
        );

        return new SummariseCurrentInformationOutput(result);
    }
}