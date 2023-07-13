namespace OpenAiSimplePipeline.OpenAI.Chains;

public class ChainableCall<TOutput> : IChainableCall<TOutput>
{
    private readonly IChainableCall<TOutput> _input;
    private readonly Func<TOutput, bool> _predicate;
    private readonly Func<TOutput, IChainableCall<TOutput>> _truePrompt;

    public ChainableCall(
        IChainableCall<TOutput> input,
        Func<TOutput,bool> predicate, 
        Func<TOutput,IChainableCall<TOutput>> truePrompt)
    {
        _input = input;
        _predicate = predicate;
        _truePrompt = truePrompt;
    }

    public async Task<TOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var initialOutput = await client.Execute(_input, token);
        if (_predicate(initialOutput))
        {
            return await _truePrompt(initialOutput).Execute(client, token);
        } 
        return initialOutput;
    }
}