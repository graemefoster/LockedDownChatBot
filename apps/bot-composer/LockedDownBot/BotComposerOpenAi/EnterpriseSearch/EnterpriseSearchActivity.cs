using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure.Identity;
using Azure.Search.Documents;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.EnterpriseSearch;
using LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseInput;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.EnterpriseSearch;

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
        var client =
            _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"]);
        var searchClient = new SearchClient(new Uri(SearchUrl.GetValue(dc.State)), Index.GetValue(dc.State),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ManagedIdentityClientId = ManagedIdentityId.GetValue(dc.State)
            }));

        var prompt = SystemPrompt.GetValue(dc.State);
        var input = string.Join('\n', Inputs.GetValue(dc.State));

        var response = await
            new ExtractKeyTermsFunction.Function()
                .Then(
                    _ => new CognitiveSearchFunction.Function(searchClient),
                    (_, output) => new CognitiveSearchFunction.Input(string.Join(' ', output.KeyTerms)))
                .Then(
                    s =>s.Resolve<SummariseContentFunction.Function>(),
                    (_, output) => new SummariseContentFunction.Input(prompt, output.Result))
                .Run(
                    client,
                    new ExtractKeyTermsFunction.Input(prompt, input),
                    cancellationToken);

        dc.State.SetValue(ResultProperty.GetValue(dc.State), response);
        return await dc.EndDialogAsync(result: response, cancellationToken);
    }
}