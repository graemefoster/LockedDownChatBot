namespace OpenAiSimplePipeline.OpenAI.Chains;

public class
    EitherChainableCall<TOutput, TFalseOutput, TTrueOutput> : IChainableCall<Either<IChainableCall<TFalseOutput>, IChainableCall<TTrueOutput>>> where TFalseOutput : class where TTrueOutput : class
{
    private readonly IChainableCall<TOutput> _input;
    private readonly Func<TOutput, bool> _predicate;
    private readonly Func<TOutput, IChainableCall<TFalseOutput>> _falsePrompt;
    private readonly Func<TOutput, IChainableCall<TTrueOutput>> _truePrompt;

    public EitherChainableCall(
        IChainableCall<TOutput> input,
        Func<TOutput,bool> predicate, 
        Func<TOutput,IChainableCall<TFalseOutput>> falsePrompt, 
        Func<TOutput,IChainableCall<TTrueOutput>> truePrompt)
    {
        _input = input;
        _predicate = predicate;
        _falsePrompt = falsePrompt;
        _truePrompt = truePrompt;
    }

    public async Task<Either<IChainableCall<TFalseOutput>, IChainableCall<TTrueOutput>>> Execute(IOpenAiClient client, CancellationToken token)
    {
        var initialOutput = await client.Execute(_input, token);
        if (_predicate(initialOutput))
        {
            return Either<IChainableCall<TFalseOutput>, IChainableCall<TTrueOutput>>.True(
                _truePrompt(initialOutput));
        } 
        return Either<IChainableCall<TFalseOutput>, IChainableCall<TTrueOutput>>.False(
            _falsePrompt(initialOutput));
    }
}