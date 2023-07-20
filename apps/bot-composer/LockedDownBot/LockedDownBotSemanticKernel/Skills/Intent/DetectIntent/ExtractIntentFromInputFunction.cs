using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;

public class ExtractIntentFromInputFunction : SemanticKernelFunction<InputOutputs.DetectIntentInput, InputOutputs.DetectIntentOutput>
{
    public static string Prompt = """
{{$Context}}

You must find the INTENT of the user's input. 

Possible intents are:
{{$Intents}}
- Unknown

Use "Unknown" if the intent is not in this list.
Respond with the intent as a single word.

{{$UserInput}}
""";

    public static ISKFunction? Register(IKernel kernel)
    {
        return SemanticKernelFunction<InputOutputs.DetectIntentInput, InputOutputs.DetectIntentOutput>.Register(kernel, Prompt);
    }
    
    protected override InputOutputs.DetectIntentOutput FromResult(InputOutputs.DetectIntentInput detectIntentInput, SKContext context)
    {
        var foundIntent = !context.Result.Equals("unknown", StringComparison.InvariantCultureIgnoreCase);
        return new InputOutputs.DetectIntentOutput()
        {
            FoundIntent = foundIntent,
            Intent = foundIntent ? context.Result : null
        };
    }

    protected override void PopulateContext(SKContext context, InputOutputs.DetectIntentInput detectIntentInput)
    {
        base.PopulateContext(context, detectIntentInput);
        context[nameof(detectIntentInput.Intents)] = string.Join("\n", detectIntentInput.Intents.Select(x => $"- {x}"));
    }
}