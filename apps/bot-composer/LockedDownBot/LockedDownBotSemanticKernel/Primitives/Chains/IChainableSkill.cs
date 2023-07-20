namespace LockedDownBotSemanticKernel.Primitives.Chains;

public interface IChainableSkill<in TInput, TOutput>
{
    Task<TOutput> Execute(SemanticKernelWrapper wrapper, TInput input, CancellationToken token);
}