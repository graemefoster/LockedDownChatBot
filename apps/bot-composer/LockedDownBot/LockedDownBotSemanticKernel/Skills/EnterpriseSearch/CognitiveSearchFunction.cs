using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.EnterpriseSearch;

public class CognitiveSearchFunction: IChainableSkill<InputOutputs.SearchInput,InputOutputs.SearchOutput>
{
    private readonly SearchClient _client;

    public CognitiveSearchFunction(SearchClient client)
    {
        _client = client;
    }
    
    public async Task<InputOutputs.SearchOutput> Execute(SemanticKernelWrapper wrapper, InputOutputs.SearchInput input, CancellationToken token)
    {
        var searchResult = (await _client
            .SearchAsync<SearchDocument>(
                input.SearchText, 
                new SearchOptions()
                {
                    Size = 1,
                    SemanticConfigurationName = "default"
                }, token)).Value.GetResults().First();
        
        return new InputOutputs.SearchOutput(searchResult.Document.GetString("content"));
    }
}