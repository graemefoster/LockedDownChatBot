using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseInput;

public static class SummariseContentFunction
{
    public record Input(string Context, string Content);
    public record Output(string Summarisation);
    
    public class Function : SemanticKernelFunction<Input, Output>
    {
        public static string Prompt = """
{{$Context}}

Given the following information, summarise it in a single paragraph.

--- INFORMATION FOLLOWS
{{$Information}}
""";

        public static ISKFunction? Register(IKernel kernel)
        {
            return SemanticKernelFunction<Input, Output>.Register(
                kernel, Prompt);
        }

        protected override Output FromResult(Input input, SKContext context)
        {
            var suggestion = context.Result;
            return new Output(context.Result);
        }
    }
}