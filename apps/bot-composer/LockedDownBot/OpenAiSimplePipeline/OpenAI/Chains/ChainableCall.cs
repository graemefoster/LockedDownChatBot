namespace OpenAiSimplePipeline.OpenAI.Chains;

public class ChainableCall<TOutput> : IChainableCall<TOutput>
{
    private readonly IChainableCall<TOutput> _input;
    private readonly Func<TOutput, bool> _predicate;
    private readonly Func<TOutput, IChainableCall<TOutput>> _falsePrompt;

    public ChainableCall(
        IChainableCall<TOutput> input,
        Func<TOutput,bool> predicate, 
        Func<TOutput,IChainableCall<TOutput>> falsePrompt)
    {
        _input = input;
        _predicate = predicate;
        _falsePrompt = falsePrompt;
    }

    public async Task<TOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var initialOutput = await client.Execute(_input, token);
        if (_predicate(initialOutput))
        {
            return await _input.Execute(client, token);
        } 
        return await _falsePrompt(initialOutput).Execute(client, token);
    }
}