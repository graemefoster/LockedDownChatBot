using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.Prompts.PromptForInput;

public record PromptOutput(string output);

public class AskForInputPrompt: IChainableCall<PromptOutput>
{
    public Task<PromptOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}