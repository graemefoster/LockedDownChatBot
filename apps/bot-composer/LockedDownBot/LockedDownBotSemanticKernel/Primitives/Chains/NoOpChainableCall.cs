
namespace LockedDownBotSemanticKernel.Primitives.Chains;

public class NoOpChainableCall<TInput, TOutput> : IChainableSkill<TInput, TOutput>
{
    private readonly TOutput _output;

    public NoOpChainableCall(TOutput output)
    {
        _output = output;
    }
    public Task<TOutput> Execute(SemanticKernelWrapper client, TInput input, CancellationToken token)
    {
        return Task.FromResult(_output);
    }
}