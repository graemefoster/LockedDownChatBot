namespace LockedDownBotSemanticKernel.Primitives.Chains;

public class
    EitherChainableCall<TInput, TOutput, TFalseInput, TFalseOutput, TTrueInput,  TTrueOutput> : 
        IChainableSkill<TInput, Either<TFalseOutput, TTrueOutput>> where TFalseOutput : class where TTrueOutput : class
{
    private readonly IChainableSkill<TInput,  TOutput> _input;
    private readonly Func<TOutput, bool> _predicate;
    private readonly Func<TInput, TOutput, TFalseInput> _falseInputFactory;
    private readonly Func<TInput, TOutput, TTrueInput> _trueInputFactory;
    private readonly Func<ISkillResolver, IChainableSkill<TFalseInput, TFalseOutput>> _falsePrompt;
    private readonly Func<ISkillResolver, IChainableSkill<TTrueInput, TTrueOutput>> _truePrompt;

    public EitherChainableCall(
        IChainableSkill<TInput, TOutput> input,
        Func<TOutput, bool> predicate,
        Func<ISkillResolver, IChainableSkill<TFalseInput, TFalseOutput>> falsePrompt,
        Func<TInput, TOutput, TFalseInput> falseInputFactory,
        Func<ISkillResolver, IChainableSkill<TTrueInput, TTrueOutput>> truePrompt,
        Func<TInput, TOutput, TTrueInput> trueInputFactory
    )
    {
        _input = input;
        _predicate = predicate;
        _falsePrompt = falsePrompt;
        _falseInputFactory = falseInputFactory;
        _truePrompt = truePrompt;
        _trueInputFactory = trueInputFactory;
    }

    public async Task<Either<TFalseOutput, TTrueOutput>> Run(
        SemanticKernelWrapper client,
        TInput input,
        CancellationToken token)
    {
        var initialOutput = await client.RunSkill(_input, input, token);
        if (_predicate(initialOutput))
        {
            var trueInput = _trueInputFactory(input, initialOutput);
            var next = _truePrompt(client);
            return Either<
                TFalseOutput, 
                TTrueOutput>.True(
                await next.Run(client, trueInput, token));
        } 

        var falseInput = _falseInputFactory(input, initialOutput);
        var falseNext = _falsePrompt(client);
        return Either<
            TFalseOutput, 
            TTrueOutput>.False(
            await falseNext.Run(client, falseInput, token));
    }
}