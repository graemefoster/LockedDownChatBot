using System.ComponentModel;
using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;

public static class GetMoreInputFromCustomerToCallInputFunction
{
    [Description("Given user input and context, and a function definition, will suggest what to ask the user to get the values for missing function parameters.")]
    public class Function : RespondToUserInputFunction.Function<ExtractInformationToCallFunction.Output, ExtractInformationToCallFunction.Output>
    {
        protected override ExtractInformationToCallFunction.Output CreateResponse(
            ExtractInformationToCallFunction.Output input,
            string suggestion)
        {
            return input with { NextRecommendation = suggestion };
        }

        protected override string DescribeSituation(ExtractInformationToCallFunction.Output input)
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