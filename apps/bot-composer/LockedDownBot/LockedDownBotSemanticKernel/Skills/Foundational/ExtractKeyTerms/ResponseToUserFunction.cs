using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;

public class ExtractKeyTermsFunction : SemanticKernelFunction<InputOutputs.ExtractKeyTermsInput, InputOutputs.ExtractKeyTermsOutput>
{
    public static string Prompt = """
{{$Context}}

Given the user input, extract the key terms as a list of words from it

--- USER INPUT FOLLOWS
{{$UserInput}}
""";

    public static ISKFunction? Register(IKernel kernel)
    {
        return SemanticKernelFunction<InputOutputs.ExtractKeyTermsInput, InputOutputs.ExtractKeyTermsOutput>.Register(kernel, Prompt);
    }

    protected override InputOutputs.ExtractKeyTermsOutput FromResult(InputOutputs.ExtractKeyTermsInput input, SKContext context)
    {
        var suggestion = context.Result;
        return new InputOutputs.ExtractKeyTermsOutput(context.Result.Split(Environment.NewLine));
    }
    
}