using Azure.AI.OpenAI;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings;

public static class GetEmbeddingsFunction
{
    public record Input(string Content);

    public record Output(string Content, IReadOnlyList<float> Embeddings);

    public class Function : IChainableSkill<Input, Output>
    {
        private readonly OpenAIClient _openAiClient;
        private readonly string _embeddingsModel;

        public Function(OpenAIClient openAiClient, string embeddingsModel)
        {
            _openAiClient = openAiClient;
            _embeddingsModel = embeddingsModel;
        }
        public async Task<Output> Run(SemanticKernelWrapper wrapper, Input input, CancellationToken token)
        {
            var embeddings = await _openAiClient.GetEmbeddingsAsync(_embeddingsModel, new EmbeddingsOptions(input.Content), token);
            return new Output(input.Content, embeddings.Value.Data[0].Embedding);
        }
    }
}
