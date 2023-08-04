using Azure.AI.OpenAI;
using Azure.Search.Documents;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.EnterpriseSearch;
using LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;
using LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;

namespace LockedDownBotSemanticKernel.Skills.ComposedSkills;

public static class VectorSearchAndSummarise
{
    public record Input(string Context, string SearchText);

    public record Output(string Result);

    public class Function : IChainableSkill<Input, Output>
    {
        private readonly OpenAIClient _openAiClient;
        private readonly string _embeddingsModel;
        private readonly SearchClient _cognitiveSearchClient;

        public Function(OpenAIClient openAiClient, string embeddingsModel, SearchClient cognitiveSearchClient)
        {
            _openAiClient = openAiClient;
            _embeddingsModel = embeddingsModel;
            _cognitiveSearchClient = cognitiveSearchClient;
        }

        public async Task<Output> Run(SemanticKernelWrapper wrapper, Input input, CancellationToken token)
        {
            var output = await new ExtractKeyTermsFunction.Function()
                .Then(_ => new GetEmbeddingsFunction.Function(_openAiClient, _embeddingsModel),
                    (i, o) => new GetEmbeddingsFunction.Input(string.Join(' ', o.KeyTerms)))
                .Then(_ => new CognitiveSearchVectorIndexFunction.Function(_cognitiveSearchClient),
                    (i, o) => new CognitiveSearchVectorIndexFunction.Input(o.Content, o.Embeddings.ToArray()))
                .Then(_ => new SummariseContentFunction.Function(),
                    (i, o) => new SummariseContentFunction.Input(input.Context, o.OriginalInput.SearchText, o.Result))
                .Run(wrapper, new ExtractKeyTermsFunction.Input(input.Context, input.SearchText), token);

            return new Output(output.Summarisation);
        }
    }
}