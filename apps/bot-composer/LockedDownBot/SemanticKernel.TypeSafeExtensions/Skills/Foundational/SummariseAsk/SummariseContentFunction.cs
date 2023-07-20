using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseAsk;

public static class SummariseAskFunction
{
    public record Input(string Context, string Content);
    public record Output(string Summarisation);
    
    public class Function : SemanticKernelFunction<Input, Output>
    {
        public static string Prompt = """
{{$Context}}

Read the following dialogue. Summarise the key ask of the User into a single sentence.

--- INFORMATION FOLLOWS
{{$Content}}
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