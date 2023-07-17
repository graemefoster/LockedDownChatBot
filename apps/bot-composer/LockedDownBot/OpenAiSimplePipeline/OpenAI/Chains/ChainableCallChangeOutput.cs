namespace OpenAiSimplePipeline.OpenAI.Chains;

public class ChainableCallChangeOutput<TInput, TOutput> : IChainableCall<TOutput>
{
    private readonly IChainableCall<TInput> _input;
    private readonly Func<TInput, IChainableCall<TOutput>> _transform;

    public ChainableCallChangeOutput(
        IChainableCall<TInput> input,
        Func<TInput, IChainableCall<TOutput>> transform)
    {
        _input = input;
        _transform = transform;
    }

    public async Task<TOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var input = await client.Execute(_input, token);
        return await _transform(input).Execute(client, token);
    }
}