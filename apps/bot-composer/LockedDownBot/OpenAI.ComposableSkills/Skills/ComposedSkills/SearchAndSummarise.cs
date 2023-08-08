using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.EnterpriseSearch;
using LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;
using LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseAsk;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;

namespace LockedDownBotSemanticKernel.Skills.ComposedSkills;

public static class SearchAndSummarise
{
    public record Input(string Context, string SearchText);

    public record Output(string Result);

    public class Function : IChainableSkill<Input, Output>
    {
        private readonly SearchClient _cognitiveSearchClient;

        public Function(SearchClient cognitiveSearchClient)
        {
            _cognitiveSearchClient = cognitiveSearchClient;
        }

        public async Task<Output> Run(ChainableSkillWrapper wrapper, Input input, CancellationToken token)
        {
            var embedding = await new ExtractKeyTermsFunction.Function()
                .Then(_ => new CognitiveSearchFunction.Function(_cognitiveSearchClient),
                    (i, o) => new CognitiveSearchFunction.Input(string.Join(' ', o.KeyTerms)))
                .Then(_ => new SummariseContentFunction.Function(),
                    (i, o) => new SummariseContentFunction.Input(input.Context, o.OriginalInput.SearchText, o.Result))
                .Run(wrapper, new ExtractKeyTermsFunction.Input(input.Context, input.SearchText), token);

            return new Output(embedding.Summarisation);
        }
    }
}