using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.TryToFindUserIntent;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiDetectIntent : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    private const string SystemPromptInternal = @"{systemPrompt}

Find the INTENT of the user's input. 
Possible intents are {intents}. Use ""Unknown"" if the intent is not in this list.
Respond with the intent as a single word. 
";

    private const string GetMoreInfoPrompt = @"{systemPrompt}

You need to find the user's intent. Possible intents are {intents}.
Given their input so-far, what would you ask the user next? 
";

    [JsonConstructor]
    public OpenAiDetectIntent(
        [CallerFilePath] string sourceFilePath = "", 
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new OpenAiClientFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }
    
    [JsonProperty("$kind")]
    public const string Kind = "OpenAiDetectIntent";
    
    [JsonProperty("systemPrompt")]
    public StringExpression SystemPrompt { get; set; }

    [JsonProperty("intents")]
    public ArrayExpression<string> Intents { get; set; }

    [JsonProperty("useAllDialogueInput")]
    public BoolExpression UseAllDialogueInput { get; set; }
    
    [JsonProperty("resultProperty")]
    public StringExpression? ResultProperty { get; set; }
    
    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromDialogueContext(dc, out var model);

        var intents = Intents.GetValue(dc.State);
        var getIntentPrompt = SystemPromptInternal
            .Replace("{systemPrompt}", SystemPrompt.GetValue(dc.State))
            .Replace(
                "{intents}",
                string.Join(',',  intents));
        
        var response = await client.GetChatCompletionsAsync(
            model,
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, getIntentPrompt),
                    new ChatMessage(ChatRole.User, dc.Context.Activity.Text)
                }
            }, cancellationToken);

        var result = response;
        if (intents.Contains(result))
        {
            var dialogueResult = new IntentResult()
            {
                Unknown = false,
                Intent = result
            };
            dc.State.SetValue(this.ResultProperty.GetValue(dc.State), dialogueResult);
            return await dc.EndDialogAsync(result: dialogueResult, cancellationToken);
        }
        
        //couldn't detect it. Let's ask GPT what to say next:
        var findMoreInfoPrompt = GetMoreInfoPrompt
            .Replace("{systemPrompt}", SystemPrompt.GetValue(dc.State))
            .Replace("{intents}", string.Join(',',  intents));
        
        var moreInfoResponse = await client.GetChatCompletionsAsync(
            model,
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, findMoreInfoPrompt),
                    new ChatMessage(ChatRole.User, dc.Context.Activity.Text)
                }
            }, cancellationToken);

        var moreInfoResult = new IntentResult()
        {
            Unknown = true,
            SuggestedPrompt = moreInfoResponse
        };
        dc.State.SetValue(ResultProperty.GetValue(dc.State), moreInfoResult);
        return await dc.EndDialogAsync(result: moreInfoResult, cancellationToken);
    }

    public class IntentResult
    {
        public bool Unknown { get; set; }
        public string Intent { get; set; }
        public string SuggestedPrompt { get; set; }
    }
}