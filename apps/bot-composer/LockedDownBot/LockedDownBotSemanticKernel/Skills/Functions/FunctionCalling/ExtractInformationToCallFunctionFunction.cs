using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;

public class ExtractInformationToCallFunctionFunction :
    SemanticKernelFunction<InputOutputs.ExtractInformationToCallFunctionFunctionInput, InputOutputs.ExtractInformationToCallFunctionFunctionOutput>
{
    public static string Prompt = """
Read the users input and respond in JSON with arguments extracted from the user's input to call the function detailed below.

- DO NOT show emotion.
- DO NOT invent parameters.
- Use "UNKNOWN" for arguments you don't know.
- ONLY respond in JSON.

{{$SystemPrompt}}

```function
{{$Function}}
```

```response
    {
        ""parameters"": {
            ""parameterName"": ""parameterValue""
        }
    }
```

--- USER INPUT FOLLOWS
{{$UserInput}}
""";

    public static ISKFunction? Register(IKernel kernel)
    {
        return SemanticKernelFunction<InputOutputs.ExtractInformationToCallFunctionFunctionInput, InputOutputs.ExtractInformationToCallFunctionFunctionOutput>.Register(kernel, Prompt);
    }

    protected override void PopulateContext(SKContext context, InputOutputs.ExtractInformationToCallFunctionFunctionInput input)
    {
        context["Function"] = JsonConvert.SerializeObject(input.FunctionDefinition, Formatting.Indented);
        base.PopulateContext(context, input);
    }

    protected override InputOutputs.ExtractInformationToCallFunctionFunctionOutput FromResult(InputOutputs.ExtractInformationToCallFunctionFunctionInput input, SKContext context)
    {
        var resultProperties = JsonConvert.DeserializeObject<InputOutputs.GptOutput>(context.Result)!.Parameters;
        var resultParameters = resultProperties.Keys;

        //check for all expected parameters:
        var exepectedParameters = input.FunctionDefinition.Parameters.Properties.Select(x => x.Key);
        var missingParameters = exepectedParameters.Where(x =>
                !resultParameters.Contains(x) ||
                resultProperties[x].ToString()!.Equals("UNKNOWN", StringComparison.InvariantCultureIgnoreCase))
            .ToHashSet();
        
        return new InputOutputs.ExtractInformationToCallFunctionFunctionOutput(
            input.FunctionDefinition,
            missingParameters.Count == 0,
            missingParameters,
            resultProperties,
            null
        );
    }
}