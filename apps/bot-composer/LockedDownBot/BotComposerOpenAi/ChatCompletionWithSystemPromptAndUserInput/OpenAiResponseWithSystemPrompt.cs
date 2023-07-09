using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.ChatCompletionWithSystemPromptAndUserInput;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiResponseWithSystemPrompt : Dialog
{
    private readonly OpenAiClientFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiResponseWithSystemPrompt(
        OpenAiClientFactory openAiClientFactory,
        [CallerFilePath] string sourceFilePath = "", 
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = openAiClientFactory;
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }
    
    [JsonProperty("$kind")]
    public const string Kind = "OpenAiResponseWithSystemPrompt";
    
    [JsonProperty("systemPrompt")]
    public StringExpression SystemPrompt { get; set; }

    [JsonProperty("useAllDialogueInput")]
    public BoolExpression UseAllDialogueInput { get; set; }
    
    [JsonProperty("resultProperty")]
    public StringExpression? ResultProperty { get; set; }
    
    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromDialogueContext(dc, out var model);
        var response = await client.GetChatCompletionsAsync(
            model,
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