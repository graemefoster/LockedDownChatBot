using System.ComponentModel;
using LockedDownBotSemanticKernel.Primitives;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseAsk;

public static class SummariseAskFunction
{
    public record Input([property:Description("Operational Context")]string Context,  [property:Description("Content to summarise")] string Content);
    public record Output(string Summarisation);
    
    [Description("Given user input and context, will summarise the user's ask in a single sentence.")]
    public class Function : ChainableSkillFunctionWithPrompt<Input, Output>
    {
        public override string Prompt(Input input) => $@"
{input.Context}

Read the following dialogue. Summarise the key ask of the User into a single sentence.

--EXAMPLES
User: When can I go to the branch?
Response: Opening hours of branches?
--- USER ASK FOLLOWS
{input.Content}
";


        protected override Output FromResult(Input input, string result)
        {
            return new Output(result);
        }
    }
}