namespace LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;

public static class InputOutputs
{
    public record ExtractFunctionInformationOutput(bool Complete, HashSet<string> MissingParameters, Dictionary<string, object> Response, string SuggestedPrompt);
    public record ExtractInformationToCallFunctionFunctionInput(string SystemPrompt, string UserInput, JsonSchemaFunctionInput FunctionDefinition);    
    public record ExtractInformationToCallFunctionFunctionOutput(JsonSchemaFunctionInput FunctionDefinition, bool MatchedAllInputParameters,  HashSet<string> MissingParameters, Dictionary<string, string> ParameterValues, string? Suggestion);
    public record GptOutput(Dictionary<string, string> Parameters);
    
    public record JsonSchemaFunctionInput(JsonSchemaFunctionInputParameters Parameters);
    public record JsonSchemaFunctionInputParameters(Dictionary<string, object> Properties);
    
}
