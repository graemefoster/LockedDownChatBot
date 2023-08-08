using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;

namespace LockedDownBotSemanticKernel.Skills.Intent.DetectIntentNextResponse;

public class GetMoreInputFromCustomerToDetectIntentInputFunctionWithPrompt : RespondToUserInputFunction.FunctionWithPrompt<ExtractIntentFromInputFunction.Input, ExtractIntentFromInputFunction.Output>
{
    protected override ExtractIntentFromInputFunction.Output FromResult(ExtractIntentFromInputFunction.Input detectIntentInput, string result)
    {
        return new ExtractIntentFromInputFunction.Output
        {
            NextRecommendation = result,
            Intent = null,
            FoundIntent = false
        };
    }

    protected override ExtractIntentFromInputFunction.Output CreateResponse(ExtractIntentFromInputFunction.Input input, string suggestion)
    {
        return new ExtractIntentFromInputFunction.Output()
        {
            FoundIntent = false,
            Intent = null,
            NextRecommendation = suggestion
        };
    }

    protected override string DescribeSituation(ExtractIntentFromInputFunction.Input input)
    {
        var intents = string.Join("\n", input.Intents.Select(x => $"- {x}"));
        
        return $"""
You are trying to detect the user's intent from. Respond with something to get the user to clarify their intent. 

{intents}
""";
        
    }

}