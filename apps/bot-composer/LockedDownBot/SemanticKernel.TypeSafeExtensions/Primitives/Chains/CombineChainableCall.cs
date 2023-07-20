namespace LockedDownBotSemanticKernel.Primitives.Chains;

public class CombineChainableCall<TInput, TFalseOutput, TTrueOutput, TNewInput, TNewOutput>: IChainableSkill<TInput, TNewOutput> where TFalseOutput : class where TTrueOutput : class
{
    private readonly IChainableSkill<TInput, Either<TFalseOutput, TTrueOutput>> _either;
    private readonly Func<TInput, TFalseOutput, TNewInput> _newFalseInputFactory;
    private readonly Func<TInput, TTrueOutput, TNewInput> _newTrueInputFactory;
    private readonly Func<ISkillResolver, IChainableSkill<TNewInput, TNewOutput>> _skill;

    public CombineChainableCall(
        IChainableSkill<TInput, Either<TFalseOutput, TTrueOutput>> either,
        Func<TInput, TFalseOutput, TNewInput> newFalseInputFactory,
        Func<TInput, TTrueOutput, TNewInput> newTrueInputFactory,
        Func<ISkillResolver, IChainableSkill<TNewInput, TNewOutput>> skill)
    {
        _either = either;
        _newFalseInputFactory = newFalseInputFactory;
        _newTrueInputFactory = newTrueInputFactory;
        _skill = skill;
    }

    public async Task<TNewOutput> ExecuteChain(SemanticKernelWrapper wrapper, TInput input, CancellationToken token)
    {
        var result = await wrapper.RunSkill(_either, input, token);
        
        var newInput = result.Result
            ? _newTrueInputFactory(input, result.ItemTrue!)
            : _newFalseInputFactory(input, result.ItemFalse!);

        return await _skill(wrapper).ExecuteChain(wrapper, newInput, token);
    }
}