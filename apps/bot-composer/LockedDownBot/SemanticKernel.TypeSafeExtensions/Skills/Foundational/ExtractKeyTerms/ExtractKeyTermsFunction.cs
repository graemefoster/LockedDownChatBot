using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;

public static class ExtractKeyTermsFunction
{
    public record Input(string Context, string UserInput);

    public record Output(string[] KeyTerms);

    public class Function : SemanticKernelFunction<Input, Output>
    {
        public override string Prompt => """
{{$Context}}

Given the user input, extract the key terms as a list of words from it

--- USER INPUT FOLLOWS
{{$UserInput}}
""";

        protected override Output FromResult(Input input, SKContext context)
        {
            var suggestion = context.Result;
            return new Output(context.Result.Split(Environment.NewLine));
        }
    }
}
