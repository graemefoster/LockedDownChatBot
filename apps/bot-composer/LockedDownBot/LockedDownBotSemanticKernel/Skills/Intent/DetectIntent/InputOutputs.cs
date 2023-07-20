using System.ComponentModel;

namespace LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;

public static class InputOutputs
{
    public record DetectIntentInput(
        [Description("Sets the context of the operation")]
        string Context,
        [Description("A list of possible intents the customer is expressing")]
        string[] Intents,
        [Description("The input from the customer")]
        string UserInput);    
    
    public class DetectIntentOutput
    {
        [Description("The Intent, or Unknown if we didn't find one")]
        public string? Intent { get; set; }

        [Description("True if we found an intent")]
        public bool FoundIntent { get; set; }

        [Description("The Intent, or Unknown if we didn't find one")]
        public string NextRecommendation { get; set; } = default!;
    }
}