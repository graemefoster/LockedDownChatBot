﻿using System.ComponentModel;
using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;

public static class ExtractIntentFromInputFunction
{
    public record Input(
        [Description("Sets the context of the operation")]
        string Context,
        [Description("A list of possible intents the customer is expressing")]
        string[] Intents,
        [Description("The input from the customer")]
        string UserInput);

    public class Output
    {
        [Description("The Intent, or Unknown if we didn't find one")]
        public string? Intent { get; set; }

        [Description("True if we found an intent")]
        public bool FoundIntent { get; set; }

        [Description("The Intent, or Unknown if we didn't find one")]
        public string NextRecommendation { get; set; } = default!;
    }

    public class Function : SemanticKernelFunction<Input, Output>
    {
        public static string Prompt = """
{{$Context}}

You must find the INTENT of the user's input. 

Possible intents are:
{{$Intents}}
- Unknown

Use "Unknown" if the intent is not in this list.
Respond with the intent as a single word.

{{$UserInput}}
""";

        public static ISKFunction? Register(IKernel kernel)
        {
            return SemanticKernelFunction<Input, Output>.Register(kernel, Prompt);
        }

        protected override Output FromResult(Input detectIntentInput, SKContext context)
        {
            var foundIntent = !context.Result.Equals("unknown", StringComparison.InvariantCultureIgnoreCase);
            return new Output()
            {
                FoundIntent = foundIntent,
                Intent = foundIntent ? context.Result : null
            };
        }

        protected override void PopulateContext(SKContext context, Input detectIntentInput)
        {
            base.PopulateContext(context, detectIntentInput);
            context[nameof(detectIntentInput.Intents)] =
                string.Join("\n", detectIntentInput.Intents.Select(x => $"- {x}"));
        }
    }
}