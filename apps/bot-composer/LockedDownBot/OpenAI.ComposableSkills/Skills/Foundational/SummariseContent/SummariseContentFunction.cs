using LockedDownBotSemanticKernel.Primitives;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;

public static class SummariseContentFunction
{
    public record Input(string Context, string OriginalAsk, string Content);

    public record Output(string Summarisation);

    public class FunctionWithPrompt : ChainableSkillFunctionWithPrompt<Input, Output>
    {
        public override string Prompt(Input input) => $@"
{input.Context}

The user asked this:
{input.OriginalAsk}

Using ONLY the following information, reply to the user with what they need to know.

--- INFORMATION FOLLOWS
{input.Content}
";

        protected override Output FromResult(Input input, string result)
        {
            return new Output(result);
        }
    }
}