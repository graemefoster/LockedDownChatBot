using LockedDownBotSemanticKernel.Primitives;

namespace LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;

public static class ExtractKeyTermsFunction
{
    public record Input(string Context, string UserInput);

    public record Output(string[] KeyTerms);

    public class FunctionWithPrompt : ChainableSkillFunctionWithPrompt<Input, Output>
    {
        public override string Prompt(Input input) => $@"
{input.Context}

Given the user input, extract the key terms separated by a newline from it.

--- USER INPUT FOLLOWS
{input.UserInput}
";

        protected override Output FromResult(Input input, string result)
        {
            return new Output(result.Split(Environment.NewLine));
        }
    }
}
