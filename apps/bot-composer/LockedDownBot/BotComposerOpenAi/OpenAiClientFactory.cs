using System.Collections.Concurrent;
using System.Collections.Immutable;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder.Dialogs;

namespace BotComposerOpenAi;

public class OpenAiClientFactory
{
    private OpenAIClient? _client;

    public OpenAIClient GetFromDialogueContext(DialogContext ctx, out string model)
    {
        var config = (ImmutableDictionary<string, object>)ctx.State["settings"];
        var endpoint = (string)config["OPENAI_Endpoint"];
        var key = (string)config["OPENAI_KEY"];

        model = (string)config["OPENAI_MODEL"];

        _client ??= new OpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(key));

        return _client;
    }
    
}