using System.ComponentModel;
using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel.Orchestration;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;

public static class SummariseContentFunction
{
    public record Input([Description("Operating Context")] string Context, [Description("What the user asked")] string OriginalAsk, [Description("Content to source resposne from")] string Content);
    public record Output([Description("Response to users question")] string Summarisation);
    
    [Description("Given user question, and some discovered content, this function will answer the user's question from the provided content.")]
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