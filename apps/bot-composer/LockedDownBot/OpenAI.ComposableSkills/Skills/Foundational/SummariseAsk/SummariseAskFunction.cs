using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseAsk;

public static class SummariseAskFunction
{
    public record Input(string Context, string Content);
    public record Output(string Summarisation);
    
    public class Function : ChainableSkillFunction<Input, Output>
    {
        public override string Prompt(Input input) => $@"
{input.Context}

Read the following dialogue. Summarise the key ask of the User into a single sentence.

--EXAMPLES
User: When can I go to the branch?
Response: Opening hours of branches?

--- USER ASK FOLLOWS
{input.Content}
""";

        protected override Output FromResult(Input input, string result)
        {
            return new Output(result);
        }
    }
}