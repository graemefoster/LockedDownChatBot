using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.EnterpriseSearch;

public static class CognitiveSearchVectorIndexFunction
{
    public record Input(string SearchText, float[] Embeddings);

    public record Output(Input OriginalInput, string Result);

    public class Function : IChainableSkill<Input, Output>
    {
        private readonly SearchClient _client;

        public Function(SearchClient client)
        {
            _client = client;
        }

        public async Task<Output> Run(SemanticKernelWrapper wrapper, Input input, CancellationToken token)
        {
            var vector = new SearchQueryVector
                { KNearestNeighborsCount = 3, Fields = "contentVector", Value = input.Embeddings };
            var searchOptions = new SearchOptions
            {
                Vector = vector,
                Size = 5,
                Select = { "metadata_storage_name", "content" },
            };

            var searchResult = (await _client
                .SearchAsync<SearchDocument>(
                    input.SearchText,
                    searchOptions, token)).Value.GetResults().First();

            return new Output(input, searchResult.Document.GetString("content"));
        }
    }
}