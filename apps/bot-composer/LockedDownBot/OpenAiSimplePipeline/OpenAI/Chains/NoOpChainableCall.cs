
namespace OpenAiSimplePipeline.OpenAI.Chains;

public class NoOpChainableCall<TOutput> : IChainableCall<TOutput>
{
    private readonly TOutput _output;

    public NoOpChainableCall(TOutput output)
    {
        _output = output;
    }
    public Task<TOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        return Task.FromResult(_output);
    }
}