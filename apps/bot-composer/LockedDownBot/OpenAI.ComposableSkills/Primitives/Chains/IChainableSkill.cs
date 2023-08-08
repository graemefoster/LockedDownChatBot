namespace LockedDownBotSemanticKernel.Primitives.Chains;

public interface IChainableSkill<in TInput, TOutput>
{
    Task<TOutput> Run(ChainableSkillWrapper wrapper, TInput input, CancellationToken token);
}