namespace LockedDownBotSemanticKernel.Skills.Foundational.SummariseInput;

public static class InputOutputs
{
    public record SummariseContent(string Context, string Content);
    public record SummariseContentOutput(string Summarisation);
}