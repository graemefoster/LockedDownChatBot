using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntentNextResponse;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;

public class GetMoreInputFromCustomerToCallFunctionFunction : ResponseToUserFunction<
    InputOutputs.ExtractInformationToCallFunctionFunctionOutput, 
    InputOutputs.ExtractInformationToCallFunctionFunctionOutput>
{
    protected override InputOutputs.ExtractInformationToCallFunctionFunctionOutput CreateResponse(
        InputOutputs.ExtractInformationToCallFunctionFunctionOutput input,
        SKContext context, 
        string suggestion)
    {
        return input with { Suggestion = suggestion };
    }

    public static ISKFunction? Register(IKernel kernel)
    {
        return SemanticKernelFunction<InputOutputs.ExtractInformationToCallFunctionFunctionInput, InputOutputs.ExtractInformationToCallFunctionFunctionOutput>.Register(kernel, Prompt);
    }
    protected override string DescribeSituation(SKContext context, InputOutputs.ExtractInformationToCallFunctionFunctionOutput input)
    {
        return $"""
We want to call the following function:

```function
{JsonConvert.SerializeObject(input.FunctionDefinition, Formatting.Indented)}
```

We are missing the following parameters.
--MISSING PARAMETERS
{string.Join('\n', input.MissingParameters)}

Please ask the user for the missing parameters.  
""";
    }
}