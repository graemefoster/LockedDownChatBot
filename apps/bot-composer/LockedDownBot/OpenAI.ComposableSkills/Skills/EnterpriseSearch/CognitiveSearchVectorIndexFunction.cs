﻿using System.ComponentModel;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.EnterpriseSearch;

public static class CognitiveSearchVectorIndexFunction
{
    public record Input(
        [property: Description("Search text")] string SearchText,
        [property: Description("Search text Embeddings")]
        float[] Embeddings);

    public record Output(
        [property: Description("Original Input")]
        Input OriginalInput,
        [property: Description("Best Search Result from index")]
        string Result);

    public class Function : IChainableSkill<Input, Output>
    {
        private readonly SearchClient _client;

        public Function(SearchClient client)
        {
            _client = client;
        }

        public async Task<Output> Run(ChainableSkillWrapper wrapper, Input input, CancellationToken token)
        {
            var vector = new SearchQueryVector
                { KNearestNeighborsCount = 3, Value = input.Embeddings };
            vector.Fields.Add("contentVector");
            var searchOptions = new SearchOptions
            {
                Size = 5,
                Select = { "metadata_storage_name", "content" },
            };
            searchOptions.Vectors.Add(vector);

            var searchResult = (await _client
                .SearchAsync<SearchDocument>(
                    input.SearchText,
                    searchOptions, token)).Value.GetResults().First();

            return new Output(input, searchResult.Document.GetString("content"));
        }
    }
}