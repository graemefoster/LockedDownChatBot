
namespace LockedDownBotSemanticKernel.Primitives;

public interface IChainableFunctionWithPrompt<TInput>
{
    string Prompt(TInput input);
}