using System.ComponentModel;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;

public static class ExtractInformationToCallFunction
{
    record GptOutput(Dictionary<string, string> Parameters);

    public record Input(
        [property:Description("Operating Context")] string Context,
        [property:Description("Conversation")] string UserInput, 
        [property:Description("JSON Schema of function")] JsonSchemaFunctionInput FunctionDefinition);

    public record Output(
        [property:Description("Operating Context")] string Context,
        [property:Description("Conversation")] string UserInput,
        [property:Description("JSON Schema of function")] JsonSchemaFunctionInput FunctionDefinition,
        [property:Description("If all parameters were matched")] bool MatchedAllInputParameters,
        [property:Description("Missing parameters")] HashSet<string> MissingParameters,
        [property:Description("Values of parameters")] Dictionary<string, string> ParameterValues,
        [property:Description("What to ask user to get the missing parameters")] string? NextRecommendation) : RespondToUserInputFunction.ISimpleRequest;

    public record JsonSchemaFunctionInput(JsonSchemaFunctionInputParameters Parameters);

    public record JsonSchemaFunctionInputParameters(Dictionary<string, object> Properties);

    [Description(
        "Given user input and context, and a function definition, will extract the parameters to call the function with.")]
    public class Function :  ChainableSkillFunctionWithPrompt<Input, Output>
    {
        public override string Prompt(Input input) => $@"
Read the users input and respond in JSON with arguments extracted from the user's input to call the function detailed below.

- DO NOT show emotion.
- DO NOT invent parameters.
- Use ""UNKNOWN"" for arguments you don't know.
- ONLY respond in JSON.

{input.Context}

```function
{JsonConvert.SerializeObject(input.FunctionDefinition, Formatting.Indented)}
```

```response
    {{
        ""parameters"": {{
            ""parameterName"": ""parameterValue""
        }}
    }}
```

--- USER INPUT FOLLOWS
{input.UserInput}
";
        
        protected override Output FromResult(Input input, string result)
        {
            var resultProperties = JsonConvert.DeserializeObject<GptOutput>(result)!.Parameters;
            var resultParameters = resultProperties.Keys;

            //check for all expected parameters:
            var exepectedParameters = input.FunctionDefinition.Parameters.Properties.Select(x => x.Key);
            var missingParameters = exepectedParameters.Where(x =>
                    !resultParameters.Contains(x) ||
                    resultProperties[x].ToString()!.Equals("UNKNOWN", StringComparison.InvariantCultureIgnoreCase))
                .ToHashSet();

            return new Output(
                input.Context,
                input.UserInput,
                input.FunctionDefinition,
                missingParameters.Count == 0,
                missingParameters,
                resultProperties,
                null
            );
        }
    }
}