using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Intent.DetectIntentNextResponse;

public class GetMoreInputFromCustomerToDetectIntentInputFunction : RespondToUserInputFunction.Function<ExtractIntentFromInputFunction.Input, ExtractIntentFromInputFunction.Output>
{
    public static ISKFunction? Register(IKernel kernel)
    {
        return RespondToUserInputFunction.Function<ExtractIntentFromInputFunction.Input, ExtractIntentFromInputFunction.Output>.Register(kernel, Prompt);
    }
    protected override ExtractIntentFromInputFunction.Output FromResult(ExtractIntentFromInputFunction.Input detectIntentInput, SKContext context)
    {
        return new ExtractIntentFromInputFunction.Output
        {
            NextRecommendation = context.Result,
            Intent = null,
            FoundIntent = false
        };
    }

    protected override ExtractIntentFromInputFunction.Output CreateResponse(ExtractIntentFromInputFunction.Input input, SKContext context, string suggestion)
    {
        return new ExtractIntentFromInputFunction.Output()
        {
            FoundIntent = false,
            Intent = null,
            NextRecommendation = suggestion
        };
    }

    protected override string DescribeSituation(SKContext context, ExtractIntentFromInputFunction.Input input)
    {
        var intents = string.Join("\n", input.Intents.Select(x => $"- {x}"));
        
        return $"""
You are trying to detect the user's intent from: 

{intents}
""";
        
    }

}