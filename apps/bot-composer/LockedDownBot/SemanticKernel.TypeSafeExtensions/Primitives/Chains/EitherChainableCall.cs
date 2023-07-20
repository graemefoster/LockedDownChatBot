namespace LockedDownBotSemanticKernel.Primitives.Chains;

public class
    EitherChainableCall<TInput, TOutput, TFalseInput, TFalseOutput, TTrueInput,  TTrueOutput> : IChainableSkill<TInput, Either<IChainableSkill<TFalseInput, TFalseOutput>, IChainableSkill<TTrueInput, TTrueOutput>>> where TFalseOutput : class where TTrueOutput : class
{
    private readonly IChainableSkill<TInput,  TOutput> _input;
    private readonly Func<TOutput, bool> _predicate;
    private readonly Func<ISkillResolver, IChainableSkill<TFalseInput, TFalseOutput>> _falsePrompt;
    private readonly Func<ISkillResolver, IChainableSkill<TTrueInput, TTrueOutput>> _truePrompt;

    public EitherChainableCall(
        IChainableSkill<TInput, TOutput> input,
        Func<TOutput, bool> predicate,
        Func<ISkillResolver, IChainableSkill<TFalseInput, TFalseOutput>> falsePrompt,
        Func<ISkillResolver, IChainableSkill<TTrueInput, TTrueOutput>> truePrompt
    )
    {
        _input = input;
        _predicate = predicate;
        _falsePrompt = falsePrompt;
        _truePrompt = truePrompt;
    }

    public async Task<Either<IChainableSkill<TFalseInput, TFalseOutput>, IChainableSkill<TTrueInput, TTrueOutput>>> Execute(
        SemanticKernelWrapper client,
        TInput input,
        CancellationToken token)
    {
        var initialOutput = await client.Execute(_input, input, token);
        if (_predicate(initialOutput))
        {
            return Either<
                IChainableSkill<TFalseInput, TFalseOutput>, 
                IChainableSkill<TTrueInput, TTrueOutput>>.True(
                _truePrompt(client));
        } 

        return Either<IChainableSkill<TFalseInput, TFalseOutput>, IChainableSkill<TTrueInput, TTrueOutput>>.False(
            _falsePrompt(client));
    }
}