namespace LockedDownBotSemanticKernel.Primitives.Chains;

public interface IChainableSkill<in TInput, TOutput>
{
    Task<TOutput> Run(SemanticKernelWrapper wrapper, TInput input, CancellationToken token);
}