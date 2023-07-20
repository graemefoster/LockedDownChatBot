using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Intent.DetectIntentNextResponse;

public class GetMoreInputFromCustomerToDetectIntentFunction : ResponseToUserFunction<DetectIntent.InputOutputs.DetectIntentInput, DetectIntent.InputOutputs.DetectIntentOutput>
{
    public static ISKFunction? Register(IKernel kernel)
    {
        return ResponseToUserFunction<DetectIntent.InputOutputs.DetectIntentInput, DetectIntent.InputOutputs.DetectIntentOutput>.Register(kernel, Prompt);
    }
    protected override DetectIntent.InputOutputs.DetectIntentOutput FromResult(DetectIntent.InputOutputs.DetectIntentInput detectIntentInput, SKContext context)
    {
        return new DetectIntent.InputOutputs.DetectIntentOutput
        {
            NextRecommendation = context.Result,
            Intent = null,
            FoundIntent = false
        };
    }

    protected override DetectIntent.InputOutputs.DetectIntentOutput CreateResponse(DetectIntent.InputOutputs.DetectIntentInput input, SKContext context, string suggestion)
    {
        return new DetectIntent.InputOutputs.DetectIntentOutput()
        {
            FoundIntent = false,
            Intent = null,
            NextRecommendation = suggestion
        };
    }

    protected override string DescribeSituation(SKContext context, DetectIntent.InputOutputs.DetectIntentInput input)
    {
        var intents = string.Join("\n", input.Intents.Select(x => $"- {x}"));
        
        return $"""
You are trying to detect the user's intent from: 

{intents}
""";
        
    }

}