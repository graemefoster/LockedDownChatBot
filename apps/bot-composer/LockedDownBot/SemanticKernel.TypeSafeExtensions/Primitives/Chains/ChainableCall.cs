using Microsoft.SemanticKernel;

namespace LockedDownBotSemanticKernel.Primitives.Chains;

public class ChainableCall<TInput, TOutput, TInput2, TOutput2> : IChainableSkill<TInput, TOutput2>
{
    private readonly IChainableSkill<TInput, TOutput> _startSkill;
    private readonly Func<TInput, TOutput, TInput2> _inputFactory;
    private readonly Func<ISkillResolver, IChainableSkill<TInput2, TOutput2>> _nextSkillFactory;

    public ChainableCall(
        IChainableSkill<TInput, TOutput> startSkill,
        Func<TInput, TOutput, TInput2> inputFactory,
        Func<ISkillResolver, IChainableSkill<TInput2, TOutput2>> nextSkillFactory)
    {
        _startSkill = startSkill;
        _inputFactory = inputFactory;
        _nextSkillFactory = nextSkillFactory;
    }

    public async Task<TOutput2> Execute(SemanticKernelWrapper client, TInput input, CancellationToken token)
    {
        var result  = await _startSkill.Execute(client, input, token);
        var nextInput = _inputFactory(input, result);
        return await _nextSkillFactory(client).Execute(client, nextInput, token);
    }
}