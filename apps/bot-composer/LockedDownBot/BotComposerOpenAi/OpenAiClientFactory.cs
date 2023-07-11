using System.Collections.Immutable;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder.Dialogs;

namespace BotComposerOpenAi;

public class OpenAiClientFactory
{
    private IOpenAiClient? _client;

    public IOpenAiClient GetFromDialogueContext(DialogContext ctx, out string model)
    {
        var config = (ImmutableDictionary<string, object>)ctx.State["settings"];
        var endpoint = (string)config["OPENAI_ENDPOINT"];
        var key = (string)config["OPENAI_KEY"];

        model = (string)config["OPENAI_MODEL"];

        _client ??= new AlternateOpenAiClient(
            new Uri(endpoint),
            new AzureKeyCredential(key));

        return _client;
    }
}

public interface IOpenAiClient
{
    Task<string> GetChatCompletionsAsync(
        string deploymentOrModelName,
        ChatCompletionsOptions chatCompletionsOptions,
        CancellationToken cancellationToken = default);
}

class AlternateOpenAiClient : IOpenAiClient
{
    private readonly OpenAIClient _client;

    public AlternateOpenAiClient(Uri uri, AzureKeyCredential azureKeyCredential)
    {
        _client = new OpenAIClient(uri, azureKeyCredential);
    }

    public async Task<string> GetChatCompletionsAsync(string deploymentOrModelName,
        ChatCompletionsOptions chatCompletionsOptions,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GetChatCompletionsAsync(deploymentOrModelName, chatCompletionsOptions, cancellationToken);
        return response.Value.Choices[0].Message.Content;
    }
}