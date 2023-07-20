namespace LockedDownBotSemanticKernel.Skills.EnterpriseSearch;

public static class InputOutputs
{
    public record SearchInput(string SearchText);
    public record SearchOutput(string Result);
}