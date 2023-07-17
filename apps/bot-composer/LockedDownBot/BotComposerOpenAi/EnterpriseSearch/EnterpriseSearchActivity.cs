using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using OpenAi.EnterpriseSearch;
using OpenAiSimplePipeline.OpenAI;

namespace BotComposerOpenAi.EnterpriseSearch;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class EnterpriseSearchActivity : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    [JsonConstructor]
    public EnterpriseSearchActivity(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new OpenAiClientFactory();
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
        var client = _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"], out var model);
        var searchClient = new SearchClient(new Uri(SearchUrl.GetValue(dc.State)), Index.GetValue(dc.State), new DefaultAzureCredential(new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = ManagedIdentityId.GetValue(dc.State)
        }));

        var prompt = SystemPrompt.GetValue(dc.State);
        var input = string.Join('\n', Inputs.GetValue(dc.State));

        var response = await EnterpriseSearchSkill.PerformSearch(searchClient, prompt, input).Execute(client, cancellationToken);

        dc.State.SetValue(ResultProperty.GetValue(dc.State), response);
        return await dc.EndDialogAsync(result: response, cancellationToken);
    }

}