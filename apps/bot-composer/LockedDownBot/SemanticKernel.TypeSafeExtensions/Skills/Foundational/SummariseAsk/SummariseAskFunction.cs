using System.ComponentModel;
using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel.Orchestration;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseAsk;

public static class SummariseAskFunction
{
    public record Input([property:Description("Operational Context")]string Context,  [property:Description("Content to summarise")] string Content);
    public record Output(string Summarisation);
    
    [Description("Given user input and context, will summarise the user's ask in a single sentence.")]
    public class Function : SemanticKernelFunction<Input, Output>
    {
        public override string Prompt => """
{{$Context}}

Read the following dialogue. Summarise the key ask of the User into a single sentence.

--- USER ASK FOLLOWS
{{$Content}}
""";


        protected override Output FromResult(Input input, SKContext context)
        {
            var suggestion = context.Result;
            return new Output(context.Result);
        }
    }
}