﻿using System.Runtime.CompilerServices;
using AdaptiveExpressions.Properties;
using Azure.AI.OpenAI;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Foundational.ChitChat;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace BotComposerOpenAi.OpenAiResponseWithSystemPrompt;

/// <summary>
/// Simple dialog. Provide a system prompt and returns a response given the User's input.
/// </summary>
public class OpenAiResponseWithSystemPromptActivity : Dialog
{
    private readonly SkillWrapperFactory _openAiClientFactory;

    [JsonConstructor]
    public OpenAiResponseWithSystemPromptActivity(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base()
    {
        _openAiClientFactory = new SkillWrapperFactory();
        RegisterSourceLocation(sourceFilePath, sourceLineNumber);
    }

    [JsonProperty("$kind")] public const string Kind = "OpenAiResponseWithSystemPrompt";

    [JsonProperty("systemPrompt")] public StringExpression SystemPrompt { get; set; }

    [JsonProperty("inputs")] public ArrayExpression<string> Inputs { get; set; }

    [JsonProperty("resultProperty")] public StringExpression? ResultProperty { get; set; }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object? options = null,
        CancellationToken cancellationToken = new())
    {
        var client = _openAiClientFactory.GetFromSettings((IDictionary<string, object>)dc.State["settings"]);

        var rawClient =
            _openAiClientFactory.GetRawClientFromSettings((IDictionary<string, object>)dc.State["settings"],
                out var model,
                out var embeddingModel);

        var input = string.Join('\n', Inputs.GetValue(dc.State));

        var response = await new ChitChatFunction.Function(rawClient, model)
            .Run(client, new ChitChatFunction.Input(
            [
                new ChatRequestSystemMessage(SystemPrompt.GetValue(dc.State)),
                new ChatRequestUserMessage(input)
            ]), cancellationToken);

        var result = response.Response;

        if (this.ResultProperty != null)
        {
            dc.State.SetValue(this.ResultProperty.GetValue(dc.State), result);
        }

        return await dc.EndDialogAsync(result: result, cancellationToken);
    }
}