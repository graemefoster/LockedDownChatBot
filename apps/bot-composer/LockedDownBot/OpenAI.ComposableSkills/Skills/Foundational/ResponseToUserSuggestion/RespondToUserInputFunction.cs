using LockedDownBotSemanticKernel.Primitives;

namespace LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;

public static class RespondToUserInputFunction
{
    public interface ISimpleRequest
    {
        string Context { get; }
        string UserInput { get; }
    }
    
    public abstract class FunctionWithPrompt<TRequest, TResponse> : ChainableSkillFunctionWithPrompt<TRequest, TResponse>
        where TRequest : ISimpleRequest 
        where TResponse : notnull
    {
        public override string Prompt(TRequest request) => $@"
{request.Context}

{DescribeSituation(request)}

Given the current conversation, ask them something to get the required information.

--- USER INPUT FOLLOWS
{request.UserInput}
";

        protected override TResponse FromResult(TRequest input, string response)
        {
            return CreateResponse(input, response);
        }

        protected abstract TResponse CreateResponse(TRequest input, string suggestion);
        protected abstract string DescribeSituation(TRequest input);
    }
}