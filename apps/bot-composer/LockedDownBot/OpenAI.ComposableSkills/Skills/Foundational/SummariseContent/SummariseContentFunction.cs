using System.ComponentModel;
using LockedDownBotSemanticKernel.Primitives;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;

public static class SummariseContentFunction
{
    public record Input([property:Description("Operating Context")] string Context, [property:Description("What the user asked")] string OriginalAsk, [property:Description("Content to source response from")] string Content);
    public record Output([property:Description("Response to users question")] string Summarisation);


    [Description("Given user question, and some discovered content, this function will answer the user's question from the provided content.")]
    public class Function : ChainableSkillFunctionWithPrompt<Input, Output>
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