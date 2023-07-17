using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using OpenAiSimplePipeline.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;
using OpenAiSimplePipeline.Skills.ExtractSearchTerms;
using OpenAiSimplePipeline.Skills.SummariseContentSkill;
using OpenAiSimplePipeline.Skills.SummariseInputSkill;

namespace OpenAi.EnterpriseSearch;

public record EnterpriseSearchOutput(string? Response);

public class EnterpriseSearchSkill : IChainableCall<EnterpriseSearchOutput>
{
    private readonly SearchClient _client;
    private readonly string _userInput;

    public EnterpriseSearchSkill(SearchClient client, string userInput)
    {
        _client = client;
        _userInput = userInput;
    }

    public async Task<EnterpriseSearchOutput> Execute(IOpenAiClient client, CancellationToken token)
    {
        var searchResult = (await _client
            .SearchAsync<SearchDocument>(_userInput, new SearchOptions() { Size = 1 }, token)).Value.GetResults().First();
        
        return new EnterpriseSearchOutput(searchResult.Document.GetString("content"));
    }

    public static IChainableCall<SummariseContentOutput> PerformSearch(SearchClient client, string systemPrompt, string userInput)
    {
        return new ExtractSearchTermsFromInput(systemPrompt, userInput)
            .Then(x => new EnterpriseSearchSkill(client, x.SuggestedTerms!))
            .Then(x => new SummariseContent(systemPrompt, x.Response!));
    }

}