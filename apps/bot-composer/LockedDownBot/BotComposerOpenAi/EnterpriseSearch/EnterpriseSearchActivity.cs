using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.ComposedSkills;
using LockedDownBotSemanticKernel.Skills.EnterpriseSearch;
using LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.EnterpriseVectorSearch;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class EnterpriseSearchActivity : Dialog
{
    private readonly SemanticKernelWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public EnterpriseSearchActivity(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SemanticKernelWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "EnterpriseSearchActivity";
    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }
    [JsonProperty("index")] public StringExpression Index { get; set; }
    [JsonProperty("managedIdentityId")] public StringExpression ManagedIdentityId { get; set; }
    [JsonProperty("searchServiceUrl")] public StringExpression SearchUrl { get; set; }
    [JsonProperty("inputs")] public ArrayExpression<string> Inputs { get; set; }
    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var settings = (IDictionary<string, object>)dc.State["settings"];
        var client =
            _openAiClientFactory.GetFromSettings(settings);

        var usingKey = settings.TryGetValue("COGNITIVE_SEARCH_KEY", out var cogSearchKey);
        var endpoint = new Uri(SearchUrl.GetValue(dc.State));
        var indexName = Index.GetValue(dc.State);
        var searchClient = !usingKey
            ? new SearchClient(endpoint, indexName, new ManagedIdentityCredential(ManagedIdentityId.GetValue(dc.State)))
            : new SearchClient(endpoint, indexName, new AzureKeyCredential((string)cogSearchKey!));

        var prompt = SystemPrompt.GetValue(dc.State);
        var input = string.Join('\n', Inputs.GetValue(dc.State));

        var response = await
            new SearchAndSummarise.Function(searchClient)
                .Run(client, new SearchAndSummarise.Input(prompt, input), cancellationToken);

        dc.State.SetValue(ResultProperty.GetValue(dc.State), response.Result);
        return await dc.EndDialogAsync(result: response, cancellationToken);
    }
}