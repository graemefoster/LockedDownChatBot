using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BotComposerOpenAi;

public class OpenAiChat : Dialog
{
    private OpenAIClient? _client;
    private string _model;

    [JsonConstructor]
    public OpenAiChat(
        [CallerFilePath] string sourceFilePath = "", 
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        // var endpoint = config.GetValue<string>("OPENAI_Endpoint");
        // var key = config.GetValue<string>("OPENAI_KEY");
        // _model =  config.GetValue<string>("OPENAI_MODEL");
        // _client = new Azure.AI.OpenAI.OpenAIClient(
        //     new Uri(key),
        //     new AzureKeyCredential(key));
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }
    
    [JsonProperty("$kind")]
    public const string Kind = "OpenAiChat";
    
    [JsonProperty("systemPrompt")]
    public StringExpression SystemPrompt { get; set; }
    
    [JsonProperty("resultProperty")]
    public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var config = (ImmutableDictionary<string, object>)dc.State["settings"];
        var endpoint = (string)config["OPENAI_Endpoint"];
        var key = (string)config["OPENAI_KEY"];
        _model =  (string)config["OPENAI_MODEL"];
        _client = _client ?? new Azure.AI.OpenAI.OpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(key));
        
        var response = await _client.GetChatCompletionsAsync(
            _model,
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, SystemPrompt.GetValue(dc.State)),
                    new ChatMessage(ChatRole.User, dc.Context.Activity.Text)
                }
            }, cancellationToken);

        
        var result = response.Value.Choices[0].Message.Content;
        if (this.ResultProperty != null)
        {
            dc.State.SetValue(this.ResultProperty.GetValue(dc.State), result);
        }

        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}