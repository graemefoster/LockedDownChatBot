using Azure;

namespace OpenAiSimplePipeline.OpenAI;

public class OpenAiClientFactory
{
    private IOpenAiClient? _client;

    public IOpenAiClient GetFromSettings(IDictionary<string, object> config, out string model)
    {
        var endpoint = (string)config["OPENAI_ENDPOINT"];
        var key = (string)config["OPENAI_KEY"];
        model = (string)config["OPENAI_MODEL"];

        _client ??= new WrappedOpenAiClient(
            new Uri(endpoint),
            new AzureKeyCredential(key),
            model);

        return _client;
    }

}