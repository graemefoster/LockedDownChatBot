using LockedDownBotSemanticKernel.Primitives;
using Microsoft.SemanticKernel.Orchestration;

namespace LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;

public static class RespondToUserInputFunction
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

    public abstract class Function<TRequest, TResponse> : SemanticKernelFunction<TRequest, TResponse> where TRequest : notnull where TResponse : notnull
    {
        public static string Prompt = """
{{$Context}}

{{$CurrentSituation}}

Given the current conversation, ask them something to get the required information.

--- USER INPUT FOLLOWS
{{$UserInput}}
""";

        protected override void PopulateContext(SKContext context, TRequest input)
        {
            base.PopulateContext(context, input);
            context["CurrentSituation"] = DescribeSituation(context, input);
        }

        protected override TResponse FromResult(TRequest input, SKContext context)
        {
            var suggestion = context.Result;
            return CreateResponse(input, context, suggestion);
        }

        protected abstract TResponse CreateResponse(TRequest input, SKContext context, string suggestion);
        protected abstract string DescribeSituation(SKContext context, TRequest input);
    }
}