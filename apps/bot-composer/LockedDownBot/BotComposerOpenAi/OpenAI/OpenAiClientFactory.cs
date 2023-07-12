using System.Collections.Immutable;
using Azure;
using Microsoft.Bot.Builder.Dialogs;

namespace BotComposerOpenAi.OpenAI;

public class OpenAiClientFactory
{
    private IOpenAiClient? _client;

    public IOpenAiClient GetFromDialogueContext(DialogContext ctx, out string model)
    {
        var config = (ImmutableDictionary<string, object>)ctx.State["settings"];
        var endpoint = (string)config["OPENAI_ENDPOINT"];
        var key = (string)config["OPENAI_KEY"];

        model = (string)config["OPENAI_MODEL"];

        _client ??= new WrappedOpenAiClient(
            new Uri(endpoint),
            new AzureKeyCredential(key));

        return _client;
    }

}