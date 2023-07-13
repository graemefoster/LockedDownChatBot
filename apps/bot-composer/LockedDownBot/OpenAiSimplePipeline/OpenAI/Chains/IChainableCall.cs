namespace OpenAiSimplePipeline.OpenAI.Chains;

public interface IChainableCall<TOutput>
{
    Task<TOutput> Execute(IOpenAiClient client, CancellationToken token);
}
