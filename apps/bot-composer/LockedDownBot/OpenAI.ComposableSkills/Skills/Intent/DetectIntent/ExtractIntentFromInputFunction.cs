using System.ComponentModel;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;

namespace LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;

public static class ExtractIntentFromInputFunction
{
    public record Input(
        [Description("Sets the context of the operation")]
        string Context,
        [Description("A list of possible intents the customer is expressing")]
        string[] Intents,
        [Description("The input from the customer")]
        string UserInput) : RespondToUserInputFunction.ISimpleRequest;

    public class Output
    {
        [Description("The Intent, or Unknown if we didn't find one")]
        public string? Intent { get; set; }

        [Description("True if we found an intent")]
        public bool FoundIntent { get; set; }

        [Description("The Intent, or Unknown if we didn't find one")]
        public string NextRecommendation { get; set; } = default!;
    }

    public class FunctionWithPrompt : ChainableSkillFunctionWithPrompt<Input, Output>
    {
        public override string Prompt(Input input) => @$"
{input.Context}

You must find the INTENT of the user's input. 

Possible intents are:
{string.Join("\n", input.Intents.Select(x => $"- {x}"))}
- Unknown

Use ""Unknown"" if the intent is not in this list.
Respond with the intent as a single word.

{input.UserInput}
";

        protected override Output FromResult(Input detectIntentInput, string result)
        {
            var foundIntent = !result.Equals("unknown", StringComparison.InvariantCultureIgnoreCase);
            return new Output()
            {
                FoundIntent = foundIntent,
                Intent = foundIntent ? result : null
            };
        }
    }
}