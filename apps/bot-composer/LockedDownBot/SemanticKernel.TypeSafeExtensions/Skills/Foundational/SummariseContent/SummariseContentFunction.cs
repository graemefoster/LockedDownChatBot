using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel.Orchestration;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;

public static class SummariseContentFunction
{
    public record Input(string Context, string OriginalAsk, string Content);
    public record Output(string Summarisation);
    
    public class Function : SemanticKernelFunction<Input, Output>
    {
        public override string Prompt => """
{{$Context}}

The user asked this:
{{$OriginalAsk}}

Using ONLY the following information, reply to the user with what they need to know.

--- INFORMATION FOLLOWS
{{$Content}}
""";

        protected override Output FromResult(Input input, SKContext context)
        {
            return new Output(context.Result);
        }
    }
}