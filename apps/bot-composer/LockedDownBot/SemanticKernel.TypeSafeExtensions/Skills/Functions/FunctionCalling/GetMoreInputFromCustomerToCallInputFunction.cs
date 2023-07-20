using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;

public static class GetMoreInputFromCustomerToCallInputFunction
{
    public class Function : RespondToUserInputFunction.Function<ExtractInformationToCallFunction.Output, ExtractInformationToCallFunction.Output>
    {
        protected override ExtractInformationToCallFunction.Output CreateResponse(
            ExtractInformationToCallFunction.Output input,
            SKContext context,
            string suggestion)
        {
            return input with { Suggestion = suggestion };
        }

        public static ISKFunction? Register(IKernel kernel)
        {
            return SemanticKernelFunction<ExtractInformationToCallFunction.Input,
                ExtractInformationToCallFunction.Output>.Register(kernel, Prompt);
        }

        protected override string DescribeSituation(SKContext context, ExtractInformationToCallFunction.Output input)
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
}