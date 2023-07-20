namespace LockedDownBotSemanticKernel.Primitives.Chains;

public interface IChainableSkill<in TInput, TOutput>
{
    Task<TOutput> ExecuteChain(SemanticKernelWrapper wrapper, TInput input, CancellationToken token);
}