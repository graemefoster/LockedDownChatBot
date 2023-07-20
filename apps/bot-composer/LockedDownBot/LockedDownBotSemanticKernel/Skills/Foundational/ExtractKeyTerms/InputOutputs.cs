namespace LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;

public static class InputOutputs
{
    public record ExtractKeyTermsInput(string Context, string Input);
    public record ExtractKeyTermsOutput(string[] KeyTerms);
}