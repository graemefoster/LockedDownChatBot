using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using LockedDownBotSemanticKernel.Memory;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Foundational.Memory;
using LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.SuggestFunctionCall;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiSuggestFunctionCallActivity : Dialog
{
    private readonly SkillWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiSuggestFunctionCallActivity(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SkillWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "OpenAiSuggestFunctionCall";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("function")] public StringExpression Function { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null,
        CancellationToken cancellationToken = new())
    {
        var settings = (IDictionary<string, object>)dc.State["settings"];

        var client = _openAiClientFactory.GetFromSettings(settings);
        var memory = _openAiClientFactory.GetMemoryFromSettings(settings);

        var conversationId = dc.GetConversationIdForDialog();
        var prompt = SystemPrompt.GetValue(dc.State);
        var function = Function.GetValue(dc.State);


        var result = await
            new RecallMemoryFunction.Function(memory, conversationId)
                .Then(_ => new ExtractInformationToCallFunction.Function(),
                    (i, o) => new ExtractInformationToCallFunction.Input(
                        prompt,
                        o.Memories.ChatsToString(),
                        JsonConvert.DeserializeObject<ExtractInformationToCallFunction.JsonSchemaFunctionInput>(
                            function)!))
                .ThenIf(
                    output => output.MissingParameters.Any(),
                    s => s
                        .Resolve<GetMoreInputFromCustomerToCallInputFunction.Function>()
                        .UpdateChatMemory(
                            (i, o) => new Chat()
                                { Actor = Conversation.AssistantActor, Message = o.NextRecommendation! }, memory,
                            conversationId))
                .Run(
                    client,
                    new RecallMemoryFunction.Input(RecallMemoryFunction.MemoryType.Last10Turns),
                    cancellationToken);

        if (ResultProperty != null)
        {
            dc.State.SetValue(ResultProperty.GetValue(dc.State), result);
        }

        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}