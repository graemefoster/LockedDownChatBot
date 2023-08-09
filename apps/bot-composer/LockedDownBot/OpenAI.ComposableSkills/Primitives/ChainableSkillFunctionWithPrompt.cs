using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Primitives;

public abstract class ChainableSkillFunctionWithPrompt<TInput, TOutput> : IChainableSkill<TInput, TOutput>, IChainableFunctionWithPrompt<TInput>
    where TInput : notnull
    where TOutput : notnull
{
    public abstract string Prompt(TInput input);

    protected abstract TOutput FromResult(TInput input, string output);

    public async Task<TOutput> Run(ChainableSkillWrapper wrapper, TInput input, CancellationToken token)
    {
        var prompt = Prompt(input);
        
        //execute against the LLM and return the result
        var output = await wrapper.RunRaw(prompt);

        return FromResult(input, output);
    }
}