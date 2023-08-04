using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel.Orchestration;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;

public static class SummariseContentFunction
{
    public record Input(string Context, string Content);
    public record Output(string Summarisation);
    
    public class Function : SemanticKernelFunction<Input, Output>
    {
        public override string Prompt => """
{{$Context}}

Given the following information, summarise all of the it in a single paragraph.

--- INFORMATION FOLLOWS
{{$Content}}
""";

        protected override Output FromResult(Input input, SKContext context)
        {
            return new Output(context.Result);
        }
    }
}