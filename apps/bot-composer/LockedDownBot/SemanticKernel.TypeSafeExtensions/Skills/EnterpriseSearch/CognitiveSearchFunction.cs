using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.EnterpriseSearch;

public static class CognitiveSearchFunction
{

    public record Input(string SearchText);
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
            var searchResult = (await _client
                .SearchAsync<SearchDocument>(
                    input.SearchText,
                    new SearchOptions()
                    {
                        Size = 1,
                        SemanticConfigurationName = "default"
                    }, token)).Value.GetResults().First();

            return new Output( input,searchResult.Document.GetString("content"));
        }
    }
}
