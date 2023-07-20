namespace LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;

public static class InputOutputs
{
    public interface IExplainCurrentSituation
    {
        string CurrentSituation { get; set; }
        string Context { get; set; }
    }

    public interface IResponseToUser
    {
        string SuggestedResponse { get; set; }
    }
    
}