namespace LockedDownBotSemanticKernel.Primitives.Chains;

public class ChainableIfCall<TInput, TInput2, TOutput> : IChainableSkill<TInput, TOutput>
{
    private readonly IChainableSkill<TInput, TOutput> _input;
    private readonly Func<TOutput, bool>? _predicate;
    private readonly Func<TInput, TOutput, TInput2> _inputFactory;
    private readonly Func<IChainableSkill<TInput2, TOutput>> _truePrompt;

    public ChainableIfCall(
        IChainableSkill<TInput, TOutput> input,
        Func<TOutput,bool>? predicate, 
        Func<TInput, TOutput, TInput2> inputFactory,
        Func<IChainableSkill<TInput2, TOutput>> truePrompt)
    {
        _input = input;
        _predicate = predicate;
        _inputFactory = inputFactory;
        _truePrompt = truePrompt;
    }

    public async Task<TOutput> Execute(SemanticKernelWrapper client, TInput input, CancellationToken token)
    {
        var initialOutput = await client.Execute(_input, input, token);
        if (_predicate == null || _predicate(initialOutput))
        {
            var newInput = _inputFactory(input, initialOutput);
            return await _truePrompt().Execute(client, newInput, token);
        } 
        return initialOutput;
    }
}
