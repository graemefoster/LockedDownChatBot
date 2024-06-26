using System.ComponentModel;
using Azure.AI.OpenAI;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.Foundational.ChitChat;

public static class ChitChatFunction
{
    public record Input(
        [property: Description("Chat Messages")]
        ChatRequestMessage[] Messages);

    public record Output([property: Description("Response")] string Response);

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

        public async Task<Output> Run(ChainableSkillWrapper wrapper, Input input, CancellationToken token)
        {
            var options = new ChatCompletionsOptions() { DeploymentName = _model };
            foreach (var msg in input.Messages) options.Messages.Add(msg);
            var response = await _openAiClient.GetChatCompletionsAsync(options, token);
            return new Output(response.Value.Choices[0].Message.Content);
        }
    }
}