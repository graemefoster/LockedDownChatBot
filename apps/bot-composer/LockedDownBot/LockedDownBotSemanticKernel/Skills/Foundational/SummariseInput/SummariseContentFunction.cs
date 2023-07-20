using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseInput;

public class SummariseContentFunction : SemanticKernelFunction<InputOutputs.SummariseContent, InputOutputs.SummariseContentOutput>
{
    public static string Prompt = """
{{$Context}}

Given the following information, summarise it in a single paragraph.

--- INFORMATION FOLLOWS
{{$Information}}
""";
    
    public static ISKFunction? Register(IKernel kernel)
    {
        return SemanticKernelFunction<InputOutputs.SummariseContent, InputOutputs.SummariseContentOutput>.Register(kernel, Prompt);
    }
    protected override InputOutputs.SummariseContentOutput FromResult(InputOutputs.SummariseContent input, SKContext context)
    {
        var suggestion = context.Result;
        return new InputOutputs.SummariseContentOutput(context.Result);
    }
}