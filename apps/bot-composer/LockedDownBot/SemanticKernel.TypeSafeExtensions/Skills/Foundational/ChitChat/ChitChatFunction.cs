using System.ComponentModel;
using Azure.AI.OpenAI;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.Foundational.ChitChat;

public static class ChitChatFunction
{
    public record Input([Description("Chat Messages")] ChatMessage[] Messages);

    public record Output([Description("Response")] string Response);

    [Description("Given OpenAI chat messages, will return a response")]
    public class Function : IChainableSkill<Input, Output>
    {
        private readonly OpenAIClient _openAiClient;
        private readonly string _model;

        public Function(OpenAIClient openAiClient, string model)
        {
            _openAiClient = openAiClient;
            _model = model;
        }

        public async Task<Output> Run(SemanticKernelWrapper wrapper, Input input, CancellationToken token)
        {
            var options = new ChatCompletionsOptions();
            foreach (var msg in input.Messages) options.Messages.Add(msg);
            var response = await _openAiClient.GetChatCompletionsAsync(_model, options, token);
            return new Output(response.Value.Choices[0].Message.Content);
        }
    }
}